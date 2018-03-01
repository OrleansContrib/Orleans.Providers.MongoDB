using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ILogger logger;
        private readonly IGrainReferenceConverter grainReferenceConverter;
        private readonly MongoDBRemindersOptions options;
        private readonly string serviceId;
        private MongoReminderCollection collection;

        public MongoReminderTable(
            ILogger<MongoReminderTable> logger,
            IOptions<MongoDBRemindersOptions> options,
            IOptions<ClusterOptions> clusterOptions,
            IGrainReferenceConverter grainReferenceConverter)
        {
            this.logger = logger;
            this.options = options.Value;
            this.serviceId = clusterOptions.Value.ServiceId.ToString();
            this.grainReferenceConverter = grainReferenceConverter;
        }

        /// <inheritdoc />
        public Task Init()
        {
            collection =
                new MongoReminderCollection(
                    options.ConnectionString,
                    options.DatabaseName,
                    options.CollectionPrefix,
                    serviceId,
                    grainReferenceConverter);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            return DoAndLog(nameof(ReadRows), () =>
            {
                return collection.ReadRow(key);
            });
        }

        /// <inheritdoc />
        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            return DoAndLog(nameof(RemoveRow), () =>
            {
                return collection.RemoveRow(grainRef, reminderName, eTag);
            });
        }

        /// <inheritdoc />
        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            return DoAndLog(nameof(ReadRow), () =>
            {
                return collection.ReadRow(grainRef, reminderName);
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
                if (begin < end)
                {
                    return collection.ReadRowsInRange(begin, end);
                }
                else
                {
                    return collection.ReadRowsOutRange(begin, end);
                }
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
                logger.Error((int)MongoProviderErrorCode.Reminders_Operations, $"ReminderTable.{actionName} failed. Exception={ex.Message}", ex);

                throw;
            }
        }
    }
}