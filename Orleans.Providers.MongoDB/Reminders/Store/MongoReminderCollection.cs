using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Reminders.Store
{
    public class MongoReminderCollection : CollectionBase<MongoReminderDocument>
    {
        private readonly IGrainReferenceConverter grainReferenceConverter;
        private readonly string serviceId;

        public MongoReminderCollection(
            string connectionsString, 
            string databaseName, 
            string serviceId,
            IGrainReferenceConverter grainReferenceConverter)
            : base(connectionsString, databaseName)
        {
            this.serviceId = serviceId;

            this.grainReferenceConverter = grainReferenceConverter;
        }

        protected override string CollectionName()
        {
            return "OrleansReminder";
        }

        protected override void SetupCollection(IMongoCollection<MongoReminderDocument> collection)
        {
            collection.Indexes.CreateOne(Index.Ascending(x => x.ServiceId).Ascending(x => x.GrainHash));
            collection.Indexes.CreateOne(Index.Ascending(x => x.ServiceId).Ascending(x => x.GrainId).Ascending(x => x.ReminderName));
        }

        public virtual async Task<ReminderTableData> ReadInRangeAsync(uint beginHash, uint endHash)
        {
            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainHash > beginHash &&
                        r.GrainHash <= endHash)
                    .ToListAsync();

            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public virtual async Task<ReminderEntry> ReadReminderRowAsync(GrainReference grainRef, string reminderName)
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

        public virtual async Task<ReminderTableData> ReadReminderRowsAsync(GrainReference grainRef)
        {
            var grainId = grainRef.ToKeyString();

            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainId == grainId)
                    .ToListAsync();
            
            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public virtual async Task<ReminderTableData> ReadOutRangeAsync(uint beginHash, uint endHash)
        {
            var reminders =
                await Collection.Find(r =>
                        (r.ServiceId == serviceId) &&
                        (r.GrainHash > beginHash || r.GrainHash <= endHash))
                    .ToListAsync();
            
            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public async Task<bool> RemoveRowAsync(GrainReference grainRef, string reminderName, string eTag)
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

        public virtual async Task<ReminderTableData> ReadReminderRowAsync(GrainReference grainRef)
        {
            var grainId = grainRef.ToKeyString();

            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainId == grainId)
                    .ToListAsync();
            
            return RemindersHelper.ProcessRemindersList(reminders, grainReferenceConverter);
        }

        public virtual Task RemoveReminderRowsAsync()
        {
            return Collection.DeleteManyAsync(r => r.ServiceId == serviceId);
        }

        public virtual async Task<string> UpsertReminderRowAsync(ReminderEntry entry)
        {
            var grainId = entry.GrainRef.ToKeyString();

            var reminder =
                await Collection.FindOneAndUpdateAsync<MongoReminderDocument>(r => 
                    r.ServiceId == serviceId &&
                    r.GrainId == grainId && 
                    r.ReminderName == entry.ReminderName,
                    Update
                        .Set(x => x.StartTime, entry.StartAt)
                        .Set(x => x.Period, entry.Period.TotalMilliseconds)
                        .Set(x => x.GrainHash, entry.GrainRef.GetUniformHashCode())
                        .Inc(x => x.Version, 1),
                    new FindOneAndUpdateOptions<MongoReminderDocument, MongoReminderDocument> { IsUpsert = true });
            
            return reminder.Version.ToString();
        }
    }
}