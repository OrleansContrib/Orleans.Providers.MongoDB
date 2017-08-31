using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Reminders.Repository
{
    public class MongoReminderTableRepository : DocumentRepository, IMongoReminderTableRepository
    {
        private const string RemindersCollectionName = "OrleansReminder";
        private const string ServiceId = "ServiceId";
        private const string GrainId = "GrainId";
        private const string ReminderName = "ReminderName";
        private readonly IGrainReferenceConverter grainReferenceConverter;

        public MongoReminderTableRepository(string connectionsString, string databaseName,
            IGrainReferenceConverter grainReferenceConverter)
            : base(connectionsString, databaseName)
        {
            this.grainReferenceConverter = grainReferenceConverter;
        }

        public async Task<ReminderTableData> ReadRangeRowsKey1Async(string serviceId, uint beginHash, uint endHash)
        {
            var collection = ReturnOrCreateRemindersCollection();
            var remindersCursor =
                await collection.FindAsync(r => r.ServiceId == serviceId && r.GrainHash > beginHash &&
                                                r.GrainHash <= endHash);

            var reminders = await remindersCursor.ToListAsync();

            return await RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public async Task<ReminderEntry> ReadReminderRowAsync(string serviceId, GrainReference grainRef,
            string reminderName)
        {
            var collection = ReturnOrCreateRemindersCollection();
            var reminderCursor = await collection.FindAsync(r =>
                r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString()
                && r.ReminderName == reminderName);

            var reminder = await reminderCursor.ToListAsync();

            return RemindersHelper.Parse(reminder.FirstOrDefault(), grainReferenceConverter);
        }

        public async Task<ReminderTableData> ReadReminderRowsAsync(string serviceId, GrainReference grainRef)
        {
            var collection = ReturnOrCreateRemindersCollection();
            var remindersCursor = await collection.FindAsync(r =>
                r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString());

            var reminders = await remindersCursor.ToListAsync();
            return await RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public async Task<ReminderTableData> ReadRangeRowsKey2Async(string serviceId, uint beginHash, uint endHash)
        {
            var collection = ReturnOrCreateRemindersCollection();
            var remindersCursor =
                await collection.FindAsync(r => r.ServiceId == serviceId &&
                                                (r.GrainHash > beginHash || r.GrainHash <= endHash));

            var reminders = await remindersCursor.ToListAsync();
            return await RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public async Task<bool> RemoveRowAsync(string serviceId, GrainReference grainRef, string reminderName,
            string eTag)
        {
            var collection = ReturnOrCreateRemindersCollection();

            var result = await
                collection.DeleteOneAsync(
                    r =>
                        r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString() &&
                        r.ReminderName == reminderName
                        && r.Version == Convert.ToInt64(eTag));

            return result.DeletedCount > 0;
        }

        public async Task<ReminderTableData> ReadReminderRowAsync(string serviceId, GrainReference grainRef)
        {
            var collection = ReturnOrCreateRemindersCollection();
            var remindersCursor = await collection.FindAsync(r =>
                r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString());

            var reminders = await remindersCursor.ToListAsync();
            return await RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public async Task RemoveReminderRowsAsync(string serviceId)
        {
            var collection = ReturnOrCreateRemindersCollection();
            await collection.DeleteManyAsync(r => r.ServiceId == serviceId);
        }

        public async Task InitTables()
        {
            if (!await CollectionExistsAsync(RemindersCollectionName))
                await ReturnOrCreateCollection(RemindersCollectionName).Indexes.CreateOneAsync(
                    Builders<BsonDocument>.IndexKeys.Ascending(ServiceId).Ascending(GrainId).Ascending(ReminderName),
                    new CreateIndexOptions {Unique = true});
        }

        public async Task<string> UpsertReminderRowAsync(
            string serviceId,
            GrainReference grainRef,
            string reminderName,
            DateTime startTime,
            TimeSpan period)
        {
            var collection = ReturnOrCreateRemindersCollection();
            var remindersCursor = await collection.FindAsync(r =>
                r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString()
                && r.ReminderName == reminderName);

            var reminders = await remindersCursor.ToListAsync();
            var reminder = reminders.FirstOrDefault();

            if (reminder == null)
            {
                // Insert
                await
                    collection.InsertOneAsync(
                        new RemindersCollection
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
            reminder.Version++;

            // Update
            var update = new UpdateDefinitionBuilder<RemindersCollection>()
                .Set(x => x.StartTime, startTime)
                .Set(x => x.Period, period.TotalMilliseconds)
                .Set(x => x.GrainHash, grainRef.GetUniformHashCode())
                .Set(x => x.Version, reminder.Version);

            await collection.UpdateOneAsync(
                r => r.ServiceId == serviceId && r.GrainId == grainRef.ToKeyString() && r.ReminderName == reminderName,
                update);
            return reminder.Version.ToString();
        }

        private IMongoCollection<RemindersCollection> ReturnOrCreateRemindersCollection()
        {
            var collection = Database.GetCollection<RemindersCollection>(RemindersCollectionName);

            if (collection != null)
                return collection;

            Database.CreateCollection(RemindersCollectionName);
            collection = Database.GetCollection<RemindersCollection>(RemindersCollectionName);

            // Todo: Create Indexs

            return collection;
        }
    }
}