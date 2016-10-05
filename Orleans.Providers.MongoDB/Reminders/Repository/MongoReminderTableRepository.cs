using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Reminders.Repository
{
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    public class MongoReminderTableRepository : DocumentRepository, IMongoReminderTableRepository
    {
        private string remindersCollectionName = "OrleansReminder";

        public MongoReminderTableRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {

        }

        public Task<ReminderTableData> ReadRangeRowsKey1(string serviceId, uint beginHash, uint endHash)
        {
            var collection = ReturnOrCreateRemindersCollection();

            var reminders =
                collection.AsQueryable()
                    .Where(r => r.ServiceId == serviceId && r.GrainHash > beginHash && r.GrainHash <= endHash)
                    .ToList();

            return this.ProcessRemindersList(reminders);
        }

        public Task<ReminderTableData> ReadRangeRowsKey2(string serviceId, uint beginHash, uint endHash)
        {
            var collection = ReturnOrCreateRemindersCollection();

            var reminders =
                collection.AsQueryable()
                    .Where(r => r.ServiceId == serviceId && (r.GrainHash > beginHash || r.GrainHash <= endHash))
                    .ToList();

            return this.ProcessRemindersList(reminders);
        }

        private Task<ReminderTableData> ProcessRemindersList(List<RemindersTable> reminders)
        {
            List<ReminderEntry> reminderEntryList = new List<ReminderEntry>();
            foreach (var reminder in reminders)
            {
                reminderEntryList.Add(this.Parse(reminder));
            }

            return Task.FromResult(new ReminderTableData(reminderEntryList));
        }

        private ReminderEntry Parse(RemindersTable reminder)
        {
            string grainId = reminder.GrainId;

            if (!string.IsNullOrEmpty(grainId))
            {
                return new ReminderEntry
                {
                    GrainRef = GrainReference.FromKeyString(grainId),
                    ReminderName = reminder.ReminderName,
                    StartAt = reminder.StartTime,
                    Period = TimeSpan.FromMilliseconds(reminder.Period),
                    ETag = reminder.Version.ToString()
                };
            }

            return null;
        }

        private IMongoCollection<RemindersTable> ReturnOrCreateRemindersCollection()
        {
            var collection = Database.GetCollection<RemindersTable>(remindersCollectionName);

            if (collection != null)
            {
                return collection;
            }

            Database.CreateCollection(remindersCollectionName);
            collection = Database.GetCollection<RemindersTable>(remindersCollectionName);

            // Todo: Create Indexs

            return collection;
        }

        public async Task<string> UpsertReminderRowAsync(
            string serviceId,
            GrainReference grainRef,
            string reminderName,
            DateTime startTime,
            TimeSpan period)
        {
            var collection = ReturnOrCreateRemindersCollection();

            var reminder =
                collection.AsQueryable()
                    .FirstOrDefault(
                        r =>
                        r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString()
                        && r.ReminderName == reminderName);

            if (reminder == null)
            {
                // Insert
                await
                    collection.InsertOneAsync(
                        new RemindersTable
                            {
                                ServiceId = serviceId,
                                GrainId = grainRef.ToKeyString(),
                                ReminderName = reminderName,
                                StartTime = startTime,
                                Period = period.TotalMilliseconds,
                                GrainHash = grainRef.GetUniformHashCode(),
                                Version = 0
                            });

                return "0";
            }
            else
            {
                reminder.Version++;

                // Update
                var update = new UpdateDefinitionBuilder<RemindersTable>()
                .Set(x => x.StartTime, startTime)
                .Set(x => x.Period, period.TotalMilliseconds)
                .Set(x => x.GrainHash, grainRef.GetUniformHashCode())
                .Set(x => x.Version, reminder.Version);

                //reminder.StartTime = startTime;
                //reminder.Period = Convert.ToInt64(period.TotalMilliseconds);
                //reminder.GrainHash = grainRef.GetUniformHashCode();
                //reminder.Version++;

                var result = await collection.UpdateOneAsync(r => r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString() && r.ReminderName == reminderName, update);
                return reminder.Version.ToString();
            }
        }
    }
}
