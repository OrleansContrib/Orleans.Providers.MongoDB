using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

// ReSharper disable RedundantIfElseBlock

namespace Orleans.Providers.MongoDB.Reminders.Store
{
    public class MongoReminderCollection : CollectionBase<MongoReminderDocument>
    {
        private static readonly FindOneAndUpdateOptions<MongoReminderDocument> UpsertReplace = new FindOneAndUpdateOptions<MongoReminderDocument> { IsUpsert = true };
        private static readonly UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };
        private readonly IGrainReferenceConverter grainReferenceConverter;
        private readonly string serviceId;
        private readonly string collectionPrefix;

        public MongoReminderCollection(
            string connectionsString, 
            string databaseName,
            string collectionPrefix,
            string serviceId,
            IGrainReferenceConverter grainReferenceConverter)
            : base(connectionsString, databaseName)
        {
            this.serviceId = serviceId;
            this.collectionPrefix = collectionPrefix;
            this.grainReferenceConverter = grainReferenceConverter;
        }

        protected override string CollectionName()
        {
            return collectionPrefix + "OrleansReminderV2";
        }

        protected override void SetupCollection(IMongoCollection<MongoReminderDocument> collection)
        {
            collection.Indexes.CreateOne(
                new CreateIndexModel<MongoReminderDocument>(
                    Index
                        .Ascending(x => x.IsDeleted)
                        .Ascending(x => x.ServiceId)
                        .Ascending(x => x.GrainHash),
                    new CreateIndexOptions
                    {
                        Name = "ByHash"
                    }));

            collection.Indexes.CreateOne(
               new CreateIndexModel<MongoReminderDocument>(
                   Index
                        .Ascending(x => x.IsDeleted)
                        .Ascending(x => x.ServiceId)
                        .Ascending(x => x.GrainId)
                        .Ascending(x => x.ReminderName),
                    new CreateIndexOptions
                    {
                        Name = "ByName"
                    }));
        }

        public virtual async Task<ReminderTableData> ReadRowsInRange(uint beginHash, uint endHash)
        {
            var reminders =
                await Collection.Find(x =>
                        x.IsDeleted == false &&
                        x.ServiceId == serviceId &&
                        x.GrainHash > beginHash &&
                        x.GrainHash <= endHash)
                    .ToListAsync();

            return new ReminderTableData(reminders.Select(x => x.ToEntry(grainReferenceConverter)));
        }

        public virtual async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            var grainId = grainRef.ToKeyString();

            var reminder =
                await Collection.Find(x =>
                        x.IsDeleted == false &&
                        x.ServiceId == serviceId &&
                        x.GrainId == grainId &&
                        x.ReminderName == reminderName)
                    .FirstOrDefaultAsync();

            return reminder?.ToEntry(grainReferenceConverter);
        }

        public virtual async Task<ReminderTableData> ReadReminderRowsAsync(GrainReference grainRef)
        {
            var grainId = grainRef.ToKeyString();

            var reminders =
                await Collection.Find(x =>
                        x.IsDeleted == false &&
                        x.ServiceId == serviceId &&
                        x.GrainId == grainId)
                    .ToListAsync();

            return new ReminderTableData(reminders.Select(x => x.ToEntry(grainReferenceConverter)));
        }

        public virtual async Task<ReminderTableData> ReadRowsOutRange(uint beginHash, uint endHash)
        {
            var reminders =
                await Collection.Find(x =>
                        (x.IsDeleted == false) &&
                        (x.ServiceId == serviceId) &&
                        (x.GrainHash > beginHash || x.GrainHash <= endHash))
                    .ToListAsync();

            return new ReminderTableData(reminders.Select(x => x.ToEntry(grainReferenceConverter)));
        }

        public virtual async Task<ReminderTableData> ReadRow(GrainReference grainRef)
        {
            var grainId = grainRef.ToKeyString();

            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainId == grainId)
                    .ToListAsync();

            return new ReminderTableData(reminders.Select(x => x.ToEntry(grainReferenceConverter)));
        }

        public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            var id = ReturnId(serviceId, grainRef, reminderName);

            try
            {
                var existingDocument =
                    await Collection.FindOneAndUpdateAsync<MongoReminderDocument, MongoReminderDocument>(x => x.Id == id && x.Etag == eTag,
                        Update.Set(x => x.IsDeleted, true),
                        UpsertReplace);

                await Collection.DeleteManyAsync(x => x.IsDeleted);

                return string.Equals(existingDocument?.ReminderName, reminderName, StringComparison.Ordinal);
            }
            catch (MongoCommandException ex)
            {
                if (ex.Message.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public virtual Task RemoveRows()
        {
            return Collection.DeleteManyAsync(r => r.ServiceId == serviceId);
        }

        public virtual async Task<string> UpsertRow(ReminderEntry entry)
        {
            var id = ReturnId(serviceId, entry.GrainRef, entry.ReminderName);

            var updatedEtag = Guid.NewGuid().ToString();
            var updateDocument = MongoReminderDocument.Create(id, serviceId, entry, updatedEtag);

            try
            {
                await Collection.ReplaceOneAsync(x => x.Id == id,
                    updateDocument,
                    Upsert);
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError.Category != ServerErrorCategory.DuplicateKey)
                {
                    throw;
                }
            }

            entry.ETag = updatedEtag;

            return entry.ETag;
        }

        private static string ReturnId(string serviceId, GrainReference grainRef, string reminderName)
        {
            return $"{serviceId}_{grainRef.ToKeyString()}_{reminderName}";
        }
    }
}