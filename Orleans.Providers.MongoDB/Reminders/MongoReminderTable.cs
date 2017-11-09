using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Reminders.Store;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
// ReSharper disable ConvertToLambdaExpression
// ReSharper disable SuggestBaseTypeForParameter

namespace Orleans.Providers.MongoDB.Reminders
{
    public class MongoReminderTable : IReminderTable
    {
        private readonly IGrainReferenceConverter grainReferenceConverter;
        private readonly ILogger logger;
        private MongoReminderCollection repository;

        public MongoReminderTable(ILogger<MongoReminderTable> logger, IGrainReferenceConverter grainReferenceConverter)
        {
            this.logger = logger;
            this.grainReferenceConverter = grainReferenceConverter;
        }

        public Task Init(GlobalConfiguration config)
        {
            var connectionString = config.DataConnectionStringForReminders;

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = config.DataConnectionString;
            }

            repository = 
                new MongoReminderCollection(connectionString,
                    MongoUrl.Create(connectionString).DatabaseName, config.ServiceId.ToString(), grainReferenceConverter);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            return DoAndLog(nameof(ReadRows), () =>
            {
                return repository.ReadRow(key);
            });
        }

        /// <inheritdoc />
        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            return DoAndLog(nameof(RemoveRow), () =>
            {
                return repository.RemoveRow(grainRef, reminderName, eTag);
            });
        }

        /// <inheritdoc />
        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            return DoAndLog(nameof(ReadRow), () =>
            {
                return repository.ReadRow(grainRef, reminderName);
            });
        }

        /// <inheritdoc />
        public Task TestOnlyClearTable()
        {
            return DoAndLog(nameof(TestOnlyClearTable), () =>
            {
                return repository.RemoveRows();
            });
        }

        /// <inheritdoc />
        public Task<string> UpsertRow(ReminderEntry entry)
        {
            return DoAndLog(nameof(UpsertRow), () =>
            {
                return repository.UpsertRow(entry);
            });
        }

        /// <inheritdoc />
        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            return DoAndLog(nameof(ReadRows), () =>
            {
                if (begin < end)
                {
                    return repository.ReadRowsInRange(begin, end);
                }
                else
                {
                    return repository.ReadRowsOutRange(begin, end);
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