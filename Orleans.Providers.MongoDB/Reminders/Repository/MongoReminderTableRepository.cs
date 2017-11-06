using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Reminders.Repository
{
    public class MongoReminderTableRepository : DocumentRepository2<RemindersCollection>
    {
        private readonly IGrainReferenceConverter grainReferenceConverter;

        public MongoReminderTableRepository(
            string connectionsString, 
            string databaseName, 
            IGrainReferenceConverter grainReferenceConverter)
            : base(connectionsString, databaseName)
        {
            this.grainReferenceConverter = grainReferenceConverter;
        }

        protected override string CollectionName()
        {
            return "OrleansReminder";
        }

        protected override async Task SetupCollectionAsync(IMongoCollection<RemindersCollection> collection)
        {
            await collection.Indexes.CreateOneAsync(Index.Ascending(x => x.ServiceId).Ascending(x => x.GrainHash));
            await collection.Indexes.CreateOneAsync(Index.Ascending(x => x.ServiceId).Ascending(x => x.GrainId).Ascending(x => x.ReminderName));
        }

        public virtual async Task<ReminderTableData> ReadRangeRowsKey1Async(string serviceId, uint beginHash, uint endHash)
        {
            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainHash > beginHash &&
                        r.GrainHash <= endHash)
                    .ToListAsync();

            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public virtual async Task<ReminderEntry> ReadReminderRowAsync(string serviceId, GrainReference grainRef, string reminderName)
        {
            var grainId = grainRef.ToKeyString();

            var reminder =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainId == grainId &&
                        r.ReminderName == reminderName)
                    .ToListAsync();

            return RemindersHelper.Parse(reminder.FirstOrDefault(), grainReferenceConverter);
        }

        public virtual async Task<ReminderTableData> ReadReminderRowsAsync(string serviceId, GrainReference grainRef)
        {
            var grainId = grainRef.ToKeyString();

            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainId == grainId)
                    .ToListAsync();
            
            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public virtual async Task<ReminderTableData> ReadRangeRowsKey2Async(string serviceId, uint beginHash, uint endHash)
        {
            var reminders =
                await Collection.Find(r =>
                        (r.ServiceId == serviceId) &&
                        (r.GrainHash > beginHash || r.GrainHash <= endHash))
                    .ToListAsync();
            
            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public async Task<bool> RemoveRowAsync(string serviceId, GrainReference grainRef, string reminderName, string eTag)
        {
            var grainId = grainRef.ToKeyString();

            var result =
                await Collection.DeleteOneAsync(r =>
                    r.ServiceId == serviceId &&
                    r.GrainId == grainId &&
                    r.ReminderName == reminderName &&
                    r.Version == Convert.ToInt64(eTag));

            return result.DeletedCount > 0;
        }

        public virtual async Task<ReminderTableData> ReadReminderRowAsync(string serviceId, GrainReference grainRef)
        {
            var grainId = grainRef.ToKeyString();

            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainId == grainId)
                    .ToListAsync();
            
            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public virtual Task RemoveReminderRowsAsync(string serviceId)
        {
            return Collection.DeleteManyAsync(r => r.ServiceId == serviceId);
        }

        public virtual async Task<string> UpsertReminderRowAsync(
            string serviceId,
            GrainReference grainRef,
            string reminderName,
            DateTime startTime,
            TimeSpan period)
        {
            var grainId = grainRef.ToKeyString();

            var reminder =
                await Collection.FindOneAndUpdateAsync<RemindersCollection>(r => 
                    r.ServiceId == serviceId &&
                    r.GrainId == grainId && 
                    r.ReminderName == reminderName,
                    Update
                        .Set(x => x.StartTime, startTime)
                        .Set(x => x.Period, period.TotalMilliseconds)
                        .Set(x => x.GrainHash, grainRef.GetUniformHashCode())
                        .Inc(x => x.Version, 1),
                    new FindOneAndUpdateOptions<RemindersCollection, RemindersCollection> { IsUpsert = true });
            
            return reminder.Version.ToString();
        }
    }
}