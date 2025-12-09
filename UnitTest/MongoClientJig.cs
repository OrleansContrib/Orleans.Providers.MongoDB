using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Providers.MongoDB.Utils;
using Xunit.Abstractions;

namespace Orleans.Providers.MongoDB.UnitTest;

/// <summary>
/// Provides utility methods and factories for interacting with MongoDB clients during unit tests.
/// </summary>
internal class MongoClientJig
{
    private readonly MongoClientSettings databaseClientSettings;
    private readonly MongoClientSettings replicaSetClientSettings;
    
    private readonly List<BsonDocument> databaseTrackedCommands;
    private readonly List<BsonDocument> replicaSetTrackedCommands;
    
    private readonly object locker = new();

    public MongoClientJig()
    {
        (databaseClientSettings, databaseTrackedCommands) = CreateClientSettings(locker, MongoDatabaseFixture.DatabaseConnectionString);
        (replicaSetClientSettings, replicaSetTrackedCommands) = CreateClientSettings(locker, MongoDatabaseFixture.ReplicaSetConnectionString);
    }

    private static (MongoClientSettings, List<BsonDocument>) CreateClientSettings(object locker, string connectionString)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        var commands = new List<BsonDocument>();

        settings.ClusterConfigurator = cb => cb.Subscribe<CommandStartedEvent>(e =>
        {
            lock (locker) commands.Add(e.Command.DeepClone().AsBsonDocument);
        });

        return (settings, commands);
    }
    
    
    public IMongoClientFactory CreateDatabaseFactory()
    {
        return new DefaultMongoClientFactory(new MongoClient(databaseClientSettings));
    }

    public IMongoClientFactory CreateReplicaSetFactory()
    {
        return new DefaultMongoClientFactory(new MongoClient(replicaSetClientSettings));
    }

    public async Task AssertQualityChecksAsync(ITestOutputHelper testOutputHelper)
    {
        var printer = new StatisticsPrinter(testOutputHelper);
        List<InspectQueryPlan> inspections = [
            printer.Collect,
            CheckNoCollectionScans,
            CheckEfficientIndexSeeks,
            CheckNoDiskUsage
        ];
        
        await Task.WhenAll(
            AssertQualityVerificationsAsync(MongoDatabaseFixture.DatabaseConnectionString, databaseTrackedCommands, inspections),
            AssertQualityVerificationsAsync(MongoDatabaseFixture.ReplicaSetConnectionString, replicaSetTrackedCommands, inspections)
        );
        
        printer.FinalizeCounts();
    }

    private static async Task AssertQualityVerificationsAsync(string databaseConnectionString, List<BsonDocument> trackedCommands, List<InspectQueryPlan> inspections)
    {
        if (trackedCommands.Count == 0)
        {
            return;
        }
        
        using var client = new MongoClient(databaseConnectionString);
        var queriesToInspect = FetchUniqueQueries(trackedCommands);

        foreach (var (sampledQuery, count) in queriesToInspect)
        {
            var database = client.GetDatabase(sampledQuery["$db"].AsString);
            sampledQuery.Remove("$db");
            
            var explainDocument = await database.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "explain", sampledQuery }
            });

            foreach (var inspection in inspections)
                inspection(explainDocument, sampledQuery, count);
        }
    }

    private static IEnumerable<(BsonDocument SampledQuery, int TotalCount)> FetchUniqueQueries(List<BsonDocument> commands)
    {
        return commands
            // find commands of interest as query fingerprints
            .Select(query=> (MongoQueryPlanFingerprint.CreateFingerprint(query), query))
            .Where(x=>!string.IsNullOrEmpty(x.Item1))
            .GroupBy(i => i.Item1, i => i.query)
            // now randomly select 5 queries to test against
            .Select(g => (g.OrderBy(_ => Guid.NewGuid()).First(), g.Count()))
            .Select(x=>(new BsonDocument(x.Item1), x.Item2));
    }

    public delegate void InspectQueryPlan(BsonDocument explainedResult, BsonDocument sampledQuery, int count);

    public class InspectionException(string message, BsonDocument explainedResult)
        : Exception($"{message}; plan: {explainedResult}");

    private void CheckNoDiskUsage(BsonDocument explainedResult, BsonDocument sampledQuery, int count)
    {
        InspectAllStages(
            GetWinningPlan(explainedResult),
            plan =>
            {
                if (plan.TryGetElement("stage", out var value) && value.Value.AsString is "GROUP" or "SORT")
                    throw new InspectionException("Potential disk usage", explainedResult);
            }
        );
    }

    private static void CheckNoCollectionScans(BsonDocument explainedResult, BsonDocument sampledQuery, int count)
    {
        InspectAllStages(
            GetWinningPlan(explainedResult),
            plan =>
            {
                if (plan.TryGetElement("stage", out var value) && value.Value.AsString == "COLLSCAN")
                    throw new InspectionException("Collection was scanned", explainedResult);
            }
        );
    }

    private static void CheckEfficientIndexSeeks(BsonDocument explainedResult, BsonDocument sampledQuery, int count)
    {
        InspectAllStages(
            GetExecutionStages(explainedResult),
            plan =>
            {
                // only look for index scans (exit early)
                if (!plan.TryGetElement("stage", out var value) || value.Value.AsString != "IXSCAN") return;
                
                // seeks should be limited to being no more than 1
                if (plan.TryGetElement("seeks", out var seeks) && seeks.Value.AsInt32 > 1)
                    throw new InspectionException("Indexes was seeked too many times", explainedResult);
            }
        );
    }
    
    private static BsonDocument GetWinningPlan(BsonDocument explainedPlan) => explainedPlan["queryPlanner"]["winningPlan"].AsBsonDocument;
    
    private static BsonDocument GetExecutionStages(BsonDocument explainedPlan) => explainedPlan["executionStats"]["executionStages"].AsBsonDocument;

    private static void InspectAllStages(BsonDocument rootPlan, Action<BsonDocument> action)
    {
        var queue = new Queue<BsonDocument>([rootPlan]);

        while (queue.TryDequeue(out var plan))
        {
            action(plan);

            if (plan.TryGetElement("inputStage", out var inputStage))
            {
                queue.Enqueue(inputStage.Value.AsBsonDocument);
            }

            if (plan.TryGetElement("inputStages", out var inputStages))
            {
                foreach (var inner in inputStages.Value.AsBsonArray)
                {
                    queue.Enqueue(inner.AsBsonDocument);
                }
            }
        }
    }

    /// <summary>
    /// Responsible for collecting, processing, and outputting statistical information
    /// about MongoDB query performance during unit tests.
    /// </summary>
    private sealed class StatisticsPrinter(ITestOutputHelper testOutputHelper)
    {
        private readonly ConcurrentDictionary<string, int> commandTotalCounts = new();
        
        public void Collect(BsonDocument explainedResult, BsonDocument sampledQuery, int count)
        {
            var executionStats = explainedResult["executionStats"].AsBsonDocument;
            var totalKeysExamined = executionStats["totalKeysExamined"].AsInt32;
            var totalDocsExamined = executionStats["totalDocsExamined"].AsInt32;
            var returned = (decimal?)executionStats["nReturned"].AsInt32;
            var stats = new
            {
                count,
                returnedToKeysExamimedRatio = totalKeysExamined != 0 ? returned / totalKeysExamined : null,
                returnedToDocsExamimedRatio = totalDocsExamined != 0 ? returned / totalDocsExamined : null,
            };
            
            testOutputHelper.WriteLine("Query statistics: {0}\n\nSampled query: {1}\n\n----\n\n", stats, sampledQuery);
            
            commandTotalCounts.AddOrUpdate(
                MongoQueryPlanFingerprint.GetCommandType(sampledQuery),
                _ => count, 
                (_, c) => count + c
            );
        }

        public void FinalizeCounts()
        {
            int accumulated = 0;
            testOutputHelper.WriteLine("COMMAND TOTAL COUNTS:");
            
            foreach (var commandTotalCount in commandTotalCounts)
            {
                testOutputHelper.WriteLine("{0}: {1}", commandTotalCount.Key, commandTotalCount.Value);
                accumulated += commandTotalCount.Value;
            }
            
            testOutputHelper.WriteLine("");
            testOutputHelper.WriteLine("COMMAND TOTAL COUNTS: {0}", accumulated);
        }
    }
}
