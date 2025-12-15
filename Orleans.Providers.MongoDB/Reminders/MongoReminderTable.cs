using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Reminders.Store;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable RedundantIfElseBlock
// ReSharper disable ConvertToLambdaExpression
// ReSharper disable SuggestBaseTypeForParameter

namespace Orleans.Providers.MongoDB.Reminders
{
    public sealed class MongoReminderTable : IReminderTable
    {
        private readonly IMongoClient mongoClient;
        private readonly ILogger logger;
        private readonly MongoDBRemindersOptions options;
        private readonly string serviceId;
        private MongoReminderCollection collection;

        public MongoReminderTable(
            IMongoClientFactory mongoClientFactory,
            ILogger<MongoReminderTable> logger,
            IOptions<MongoDBRemindersOptions> options,
            IOptions<ClusterOptions> clusterOptions)
        {
            this.mongoClient = mongoClientFactory.Create(options.Value, "Reminder");
            this.logger = logger;
            this.options = options.Value;
            this.serviceId = clusterOptions.Value.ServiceId ?? string.Empty;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            collection =
                new MongoReminderCollection(
                    mongoClient,
                    options.DatabaseName,
                    options.CollectionPrefix,
                    options.CollectionConfigurator,
                    options.CreateShardKeyForCosmos,
                    options.PurgeLegacyIndexes,
                    serviceId);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<ReminderTableData> ReadRows(GrainId grainId)
        {
            return DoAndLog(nameof(ReadRows), () =>
            {
                return collection.ReadRow(grainId);
            });
        }

        /// <inheritdoc />
        public Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            CheckGrainId(grainId);
            
            return DoAndLog(nameof(RemoveRow), () =>
            {
                return collection.RemoveRow(grainId, reminderName, eTag);
            });
        }

        /// <inheritdoc />
        public Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
        {
            CheckGrainId(grainId);
            
            return DoAndLog(nameof(ReadRow), () =>
            {
                return collection.ReadRow(grainId, reminderName);
            });
        }

        /// <inheritdoc />
        public Task TestOnlyClearTable()
        {
            return DoAndLog(nameof(TestOnlyClearTable), () =>
            {
                return collection.RemoveRows();
            });
        }

        /// <inheritdoc />
        public Task<string> UpsertRow(ReminderEntry entry)
        {
            CheckGrainId(entry.GrainId);
            
            return DoAndLog(nameof(UpsertRow), () =>
            {
                return collection.UpsertRow(entry);
            });
        }

        /// <inheritdoc />
        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            return DoAndLog(nameof(ReadRows), () =>
            {
                return collection.ReadRows(begin, end);
            });
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            logger.LogDebug($"ReminderTable.{actionName} called.");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogError((int)MongoProviderErrorCode.Reminders_Operations, ex, $"ReminderTable.{actionName} failed. Exception={ex.Message}");

                throw;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckGrainId(GrainId grainId)
        {
            if (grainId.IsDefault)
                throw new InvalidOperationException(
                    "Grain identifier is default. Was the reminder registry called from a client application?");
        }
    }
}