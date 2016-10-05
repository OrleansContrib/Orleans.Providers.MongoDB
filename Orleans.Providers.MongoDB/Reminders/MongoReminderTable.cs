using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Reminders
{
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
            serviceId = config.ServiceId.ToString();
            this.logger = traceLogger;

            string connectionString = config.DataConnectionStringForReminders;

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = config.DataConnectionString;
            }
            
            repository = new MongoReminderTableRepository(connectionString, MongoUrl.Create(config.DataConnectionString).DatabaseName);

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
                    // ReadRangeRowsKey1
                    return await this.repository.ReadRangeRowsKey1(this.serviceId, begin, end);
                }
                else
                {
                    // ReadRangeRowsKey2
                    return await this.repository.ReadRangeRowsKey2(this.serviceId, begin, end);
                }
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

        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            throw new NotImplementedException();
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
                return await this.repository.UpsertReminderRowAsync(
                    serviceId,
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
        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            throw new NotImplementedException();
        }

        public Task TestOnlyClearTable()
        {
            throw new NotImplementedException();
        }
    }
}
