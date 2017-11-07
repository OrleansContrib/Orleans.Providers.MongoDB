using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Reminders.Repository;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace Orleans.Providers.MongoDB.Reminders
{
    public class MongoReminderTable : IReminderTable
    {
        private readonly IGrainReferenceConverter grainReferenceConverter;
        private Logger logger;
        private MongoReminderCollection repository;
        private string serviceId;

        public MongoReminderTable(IGrainReferenceConverter grainReferenceConverter)
        {
            this.grainReferenceConverter = grainReferenceConverter;
        }

        public Task Init(GlobalConfiguration config, Logger traceLogger)
        {
            serviceId = config.ServiceId.ToString();
            logger = traceLogger;

            var connectionString = config.DataConnectionStringForReminders;

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = config.DataConnectionString;
            }

            repository = new MongoReminderCollection(connectionString,
                MongoUrl.Create(connectionString).DatabaseName, grainReferenceConverter);

            return Task.CompletedTask;
        }

        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            return DoLogged(nameof(ReadRows), 
                () => repository.ReadReminderRowAsync(serviceId, key));
        }

        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            return DoLogged(nameof(RemoveRow),
                () => repository.RemoveRowAsync(serviceId, grainRef, reminderName, eTag));
        }

        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            return DoLogged(nameof(ReadRow),
                () => repository.ReadReminderRowAsync(serviceId, grainRef, reminderName));
        }

        public Task TestOnlyClearTable()
        {
            return DoLogged(nameof(TestOnlyClearTable),
                async () =>
                {
                    await repository.RemoveReminderRowsAsync(serviceId);

                    return true;
                });
        }

        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            return DoLogged(nameof(ReadRows),
                () =>
                {
                    if (begin < end)
                    {
                        return repository.ReadInRangeAsync(serviceId, begin, end);
                    }
                    else
                    {
                        return repository.ReadOutRangeAsync(serviceId, begin, end);
                    }
                });
        }

        public Task<string> UpsertRow(ReminderEntry entry)
        {
            return DoLogged(nameof(UpsertRow), () => 
                repository.UpsertReminderRowAsync(serviceId,
                    entry.GrainRef,
                    entry.ReminderName,
                    entry.StartAt,
                    entry.Period));
        }

        private async Task<T> DoLogged<T>(string actionName, Func<Task<T>> action)
        {
            if (logger.IsVerbose3)
            {
                logger.Verbose3($"ReminderTable.{actionName} called with serviceId {serviceId}.");
            }

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                {
                    logger.Verbose($"ReminderTable.{actionName} failed: {ex}");
                }

                throw;
            }
        }
    }
}