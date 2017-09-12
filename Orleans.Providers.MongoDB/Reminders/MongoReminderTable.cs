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

        private IMongoReminderTableRepository repository;
        private string serviceId;

        public MongoReminderTable(IGrainReferenceConverter grainReferenceConverter)
        {
            this.grainReferenceConverter = grainReferenceConverter;
        }

        public async Task Init(GlobalConfiguration config, Logger traceLogger)
        {
            serviceId = config.ServiceId.ToString();
            logger = traceLogger;

            var connectionString = config.DataConnectionStringForReminders;

            if (string.IsNullOrEmpty(connectionString))
                connectionString = config.DataConnectionString;

            repository = new MongoReminderTableRepository(connectionString,
                MongoUrl.Create(connectionString).DatabaseName, grainReferenceConverter);
            await repository.InitTables();
        }

        public async Task<ReminderTableData> ReadRows(GrainReference key)
        {
            try
            {
                if (logger.IsVerbose3)
                    logger.Verbose3(
                        string.Format(
                            "ReminderTable.ReadRows called with serviceId {0}.",
                            serviceId));

                return await repository.ReadReminderRowAsync(serviceId, key);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("ReminderTable.ReadRows failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        ///     Return all rows that have their GrainReference's.GetUniformHashCode() in the range (start, end]
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            if (logger.IsVerbose3)
                logger.Verbose3(
                    $"ReminderTable.ReadRows called with serviceId {serviceId}.");

            try
            {
                if ((int) begin < (int) end)
                    return await repository.ReadRangeRowsKey1Async(serviceId, begin, end);

                // ReadRangeRowsKey2Async
                return await repository.ReadRangeRowsKey2Async(serviceId, begin, end);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("ReminderTable.ReadRows failed: {0}", ex);

                throw;
            }
        }

        public async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            if (logger.IsVerbose3)
                logger.Verbose3(
                    $"ReminderTable.ReadRow called with serviceId {serviceId}.");

            try
            {
                return await repository.ReadReminderRowAsync(serviceId, grainRef, reminderName);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("ReminderTable.ReadRow failed: {0}", ex);

                throw;
            }
        }

        public async Task<string> UpsertRow(ReminderEntry entry)
        {
            if (logger.IsVerbose3)
                logger.Verbose3(
                    $"ReminderTable.UpsertRow called with serviceId {serviceId}.");

            try
            {
                return await repository.UpsertReminderRowAsync(serviceId,
                    entry.GrainRef,
                    entry.ReminderName,
                    entry.StartAt,
                    entry.Period);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("ReminderTable.UpsertRow failed: {0}", ex);

                throw;
            }
        }

        /// <summary>Remove a row from the table.</summary>
        /// <param name="grainRef"></param>
        /// <param name="reminderName"></param>
        /// ///
        /// <param name="eTag"></param>
        /// <returns>
        ///     true if a row with <paramref name="grainRef" /> and <paramref name="reminderName" /> existed and was removed
        ///     successfully, false otherwise
        /// </returns>
        public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            if (logger.IsVerbose3)
                logger.Verbose3(
                    $"ReminderTable.RemoveRow called with serviceId {serviceId}.");

            try
            {
                return await repository.RemoveRowAsync(serviceId, grainRef, reminderName, eTag);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("ReminderTable.RemoveRow failed: {0}", ex);

                throw;
            }
        }

        public async Task TestOnlyClearTable()
        {
            if (logger.IsVerbose3)
                logger.Verbose3(
                    $"ReminderTable.TestOnlyClearTable called with serviceId {serviceId}.");

            try
            {
                await repository.RemoveReminderRowsAsync(serviceId);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("ReminderTable.TestOnlyClearTable failed: {0}", ex);

                throw;
            }
        }
    }
}