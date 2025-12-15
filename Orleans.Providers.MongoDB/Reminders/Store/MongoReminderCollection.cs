using System;
using System.Collections.Generic;
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
        private readonly bool removeAllLegacyIndexes;

        public MongoReminderCollection(IMongoClient mongoClient,
            string databaseName,
            string collectionPrefix,
            Action<MongoCollectionSettings> collectionConfigurator,
            bool createShardKey,
            bool removeAllLegacyIndexes,
            string serviceId)
            : base(mongoClient, databaseName, collectionConfigurator, createShardKey)
        {
            this.serviceId = serviceId;
            this.collectionPrefix = collectionPrefix;
            this.removeAllLegacyIndexes = removeAllLegacyIndexes;
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
                if (!ex.IsDuplicateIndex())
                {
                    throw;
                }
                
                // ignore all exceptions that a pre-existing index has already been created. These indexes may
                // have already been placed by Mongo Database administrators (e.g. "QueryOptimization_Finding1").
                // in the future, if need to be culled, only try collect library generated indexes.
            }

            List<string> indexesToRemove =
            [
                // ByHash definitions.
                "ByHash",
#pragma warning disable CS0618 // Type or member is obsolete
                $"{collection.GetFieldName(r => r.IsDeleted)}_1"
                + $"_{collection.GetFieldName(r => r.ServiceId)}_1"
                + $"_{collection.GetFieldName(r => r.GrainHash)}_1"
#pragma warning restore CS0618 // Type or member is obsolete
            ];

            if (removeAllLegacyIndexes)
            {
                // these indexes are safe to keep and redundant.
                // However, if a rolling deployment is made with silos with Orleans.Providers.MongoDB on version <=9.3.0,
                // there is a high chance of collection scans impacting the full cluster. 
                // See: https://github.com/OrleansContrib/Orleans.Providers.MongoDB/pull/154
                indexesToRemove.AddRange([
                    // ByName definition
                    "ByName",
#pragma warning disable CS0618 // Type or member is obsolete
                    $"{collection.GetFieldName(r => r.IsDeleted)}_1"
                    + $"_{collection.GetFieldName(r => r.ServiceId)}_1"
                    + $"_{collection.GetFieldName(r => r.GrainId)}_1"
                    + $"_{collection.GetFieldName(r => r.ReminderName)}_1",
#pragma warning restore CS0618 // Type or member is obsolete
                ]);
            }

            foreach (var indexToRemove in indexesToRemove)
            {
                // best effort drop
                try
                {
                    collection.Indexes.DropOne(indexToRemove);
                }
                catch
                {
                    // Ignore since failure to drop a legacy index should not affect Silo operations under any circumstances.
                    // see for motivation: https://github.com/OrleansContrib/Orleans.Providers.MongoDB/pull/154#discussion_r2617770110
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

            var useUpsert = entry.ETag != null || !await TryInsertOneAsync();

            if (useUpsert)
            {
                // see comments in TryInsertOneAsync to determine when selecting the upsert.
                await Collection.ReplaceOneAsync(x => x.Id == id, document, UpsertReplace);
            }

            return entry.ETag = document.Etag;

            async Task<bool> TryInsertOneAsync()
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
                catch (MongoException ex)
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