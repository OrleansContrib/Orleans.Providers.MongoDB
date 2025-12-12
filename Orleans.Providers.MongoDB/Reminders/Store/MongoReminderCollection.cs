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
        private readonly string serviceId;
        private readonly string collectionPrefix;

        public MongoReminderCollection(
            IMongoClient mongoClient,
            string databaseName,
            string collectionPrefix,
            Action<MongoCollectionSettings> collectionConfigurator,
            bool createShardKey,
            string serviceId)
            : base(mongoClient, databaseName, collectionConfigurator, createShardKey)
        {
            this.serviceId = serviceId;
            this.collectionPrefix = collectionPrefix;
        }

        protected override string CollectionName()
        {
            return collectionPrefix + "OrleansReminderV2";
        }

        protected override void SetupCollection(IMongoCollection<MongoReminderDocument> collection)
        {
            var byGrainHashDefinition =
                Index
                    .Ascending(x => x.ServiceId)
                    .Ascending(x => x.GrainHash);
            try
            {
                collection.Indexes.CreateOne(
                    new CreateIndexModel<MongoReminderDocument>(byGrainHashDefinition,
                        new CreateIndexOptions
                        {
                            Name = "ByGrainHash"
                        }));
            }
            catch (MongoCommandException ex)
            {
                if (ex.CodeName == "IndexOptionsConflict")
                {
                    collection.Indexes.CreateOne(new CreateIndexModel<MongoReminderDocument>(byGrainHashDefinition));
                }
            }
            
            // best effort: remove indexes from previous releases
            try
            {
                collection.Indexes.DropOne("ByHash");
            }
            catch (Exception)
            {
                try
                {
                    // by convention, the definition would have auto-generated this naming by convention
                    collection.Indexes.DropOne("IsDeleted_1_ServiceId_1_GrainHash_1");
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public virtual async Task<ReminderTableData> ReadRows(uint beginHash, uint endHash)
        {
            // (begin) is beginning exclusive of hash
            // [end] is the stop point, inclusive of hash
            var filter = beginHash < endHash
                ? Builders<MongoReminderDocument>.Filter.Where(x =>
                    x.ServiceId == serviceId &&
                    //       (begin)>>>>>>[end]
                    x.GrainHash > beginHash && x.GrainHash <= endHash
                )
                : Builders<MongoReminderDocument>.Filter.Where(x =>
                    x.ServiceId == serviceId &&
                    // >>>>>>[end]         (begin)>>>>>>>
                    (x.GrainHash <= endHash || x.GrainHash > beginHash)
                );
            var reminders = await Collection.Find(filter).ToListAsync();

            return new ReminderTableData(reminders.Select(x => x.ToEntry()));
        }

        public virtual async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
        {
            var id = ReturnId(serviceId, grainId, reminderName);
            var reminder =
                await Collection.Find(x => x.Id == id)
                    .FirstOrDefaultAsync();

            return reminder?.ToEntry();
        }

        public virtual async Task<ReminderTableData> ReadRow(GrainId grainId)
        {
            var reminders =
                await Collection.Find(r =>
                        r.ServiceId == serviceId &&
                        r.GrainHash == grainId.GetUniformHashCode() &&
                        r.GrainId == grainId.ToString())
                    .ToListAsync();

            return new ReminderTableData(reminders.Select(x => x.ToEntry()));
        }

        public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            var id = ReturnId(serviceId, grainId, reminderName);

            var deleteResult = await Collection.DeleteOneAsync(x => x.Id == id && x.Etag == eTag);
            return deleteResult.DeletedCount > 0;
        }

        public virtual Task RemoveRows()
        {
            // note: only used and called by the test harness
            return Collection.DeleteManyAsync(r => r.ServiceId == serviceId);
        }

        public virtual async Task<string> UpsertRow(ReminderEntry entry)
        {
            var id = ReturnId(serviceId, entry.GrainId, entry.ReminderName);
            var document = MongoReminderDocument.Create(id, serviceId, entry, Guid.NewGuid().ToString());

            var useUpsert = entry.ETag != null || !await TryEmployInsertionStrategyAsync();

            if (useUpsert)
            {
                // see comments in TryEmployInsertionStrategyAsync to determine when selecting the upsert.
                await Collection.ReplaceOneAsync(x => x.Id == id, document, UpsertReplace);
            }

            return entry.ETag = document.Etag;

            async Task<bool> TryEmployInsertionStrategyAsync()
            {
                // when the etag is null, it is a strong indicator that an insertion is only necessary
                // as it is brand new.
                // In the unlikely event that this assumption is incorrect, mongo will throw a conflict.
                try
                {
                    // insertion is a lot faster than doing a search in Mongo
                    await Collection.InsertOneAsync(document);
                    return true;
                }
                catch (MongoCommandException ex)
                {
                    if (ex.IsDuplicateKey())
                    {
                        // we got a conflict, so some other thread inserted with no etag.
                        // this is highly improbable in production workloads, but is a guard on the standard
                        // test suites from Orleans to assert contract behavior.
                        return false;
                    }

                    throw;
                }
            }
        }

        private static string ReturnId(string serviceId, GrainId grainId, string reminderName)
        {
            return $"{serviceId}_{grainId}_{reminderName}";
        }
    }
}