namespace Orleans.Providers.MongoDB.Reminders
{
    using System;
    using System.Threading.Tasks;

    using global::MongoDB.Driver;

    using Orleans.Providers.MongoDB.Reminders.Repository;
    using Orleans.Runtime;
    using Orleans.Runtime.Configuration;

    public class MongoReminderTable : IReminderTable
    {

        private IMongoReminderTableRepository repository;
        private string serviceId;

        private TraceLogger logger;

        public Task Init(GlobalConfiguration config, TraceLogger traceLogger)
        {
            this.serviceId = config.ServiceId.ToString();
            this.logger = traceLogger;

            string connectionString = config.DataConnectionStringForReminders;

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = config.DataConnectionString;
            }

            this.repository = new MongoReminderTableRepository(connectionString, MongoUrl.Create(config.DataConnectionString).DatabaseName);

            return TaskDone.Done;
        }

        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return all rows that have their GrainReference's.GetUniformHashCode() in the range (start, end]
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3(
                    string.Format(
                        "ReminderTable.ReadRows called with serviceId {0}.",
                        this.serviceId));
            }

            try
            {
                if ((int)begin < (int)end)
                {
                    // ReadRangeRowsKey1Async
                    return await this.repository.ReadRangeRowsKey1Async(this.serviceId, begin, end);
                }

                // ReadRangeRowsKey2Async
                return await this.repository.ReadRangeRowsKey2Async(this.serviceId, begin, end);
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("ReminderTable.ReadRows failed: {0}", ex);
                }

                throw;
            }
        }

        public async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3(
                    string.Format(
                        "ReminderTable.ReadRow called with serviceId {0}.",
                        this.serviceId));
            }

            try
            {
                return await this.repository.ReadReminderRowAsync(this.serviceId, grainRef, reminderName);
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("ReminderTable.ReadRow failed: {0}", ex);
                }

                throw;
            }
        }

        public async Task<string> UpsertRow(ReminderEntry entry)
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3(
                    string.Format(
                        "ReminderTable.UpsertRow called with serviceId {0}.",
                        this.serviceId));
            }

            try
            {
                return await this.repository.UpsertReminderRowAsync(this.serviceId,
                    entry.GrainRef,
                    entry.ReminderName,
                    entry.StartAt,
                    entry.Period);
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("ReminderTable.UpsertRow failed: {0}", ex);
                }

                throw;
            }
        }

        /// <summary>Remove a row from the table.</summary>
        /// <param name="grainRef"></param>
        /// <param name="reminderName"></param>
        ///             /// <param name="eTag"></param>
        /// <returns>true if a row with <paramref name="grainRef" /> and <paramref name="reminderName" /> existed and was removed successfully, false otherwise</returns>
        public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3(
                    string.Format(
                        "ReminderTable.RemoveRow called with serviceId {0}.",
                        this.serviceId));
            }

            try
            {
                return await this.repository.RemoveRowAsync(this.serviceId, grainRef, reminderName, eTag);
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("ReminderTable.RemoveRow failed: {0}", ex);
                }

                throw;
            }
        }

        public async Task TestOnlyClearTable()
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3(
                    string.Format(
                        "ReminderTable.TestOnlyClearTable called with serviceId {0}.",
                        this.serviceId));
            }

            try
            {
                await this.repository.RemoveReminderRowsAsync(serviceId);
            }
            catch (Exception ex)
            {

                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("ReminderTable.TestOnlyClearTable failed: {0}", ex);
                }

                throw;
            }

        }
    }
}
