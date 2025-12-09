using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Providers.MongoDB.Utils;

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

    public Task AssertQualityChecksAsync()
    {
        List<InspectQueryPlan> inspections = [
            CheckNoCollectionScans,
            CheckEfficientIndexSeeks,
        ];
        
        return Task.WhenAll(
            AssertQualityVerificationsAsync(MongoDatabaseFixture.DatabaseConnectionString, databaseTrackedCommands, inspections),
            AssertQualityVerificationsAsync(MongoDatabaseFixture.ReplicaSetConnectionString, replicaSetTrackedCommands, inspections)
        );
    }

    private async Task AssertQualityVerificationsAsync(string databaseConnectionString, List<BsonDocument> trackedCommands, List<InspectQueryPlan> inspections)
    {
        if (trackedCommands.Count == 0)
        {
            return;
        }
        
        using var client = new MongoClient(databaseConnectionString);
        var queriesToInspect = FetchUniqueQueries(trackedCommands);

        foreach (var query in queriesToInspect)
        {
            var database = client.GetDatabase(query["$db"].AsString);
            query.Remove("$db");
            
            var explainDocument = await database.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "explain", query }
            });

            foreach (var inspection in inspections)
                inspection(explainDocument, query);
        }
    }

    private static IEnumerable<BsonDocument> FetchUniqueQueries(List<BsonDocument> commands)
    {
        return commands
            // find commands of interest as query fingerprints
            .Select(query=> (MongoQueryPlanFingerprint.CreateFingerprint(query), query))
            .Where(x=>!string.IsNullOrEmpty(x.Item1))
            .GroupBy(i => i.Item1, i => i.query)
            // now randomly select 5 queries to test against
            .SelectMany(g => g.OrderBy(_ => Guid.NewGuid()).Take(5))
            .Select(x=>new BsonDocument(x));
    }

    public delegate void InspectQueryPlan(BsonDocument explainedResult, BsonDocument originalQuery);

    public class InspectionException(string message, BsonDocument explainedResult)
        : Exception($"{message}; plan: {explainedResult}");

    private static void CheckNoCollectionScans(BsonDocument explainedResult, BsonDocument originalQuery)
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

    private static void CheckEfficientIndexSeeks(BsonDocument explainedResult, BsonDocument sampledQuery)
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
}
