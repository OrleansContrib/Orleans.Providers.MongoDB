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
                await Collection.Find(Filter.Eq(x => x.Id, id))
                    .FirstOrDefaultAsync();

            return reminder?.ToEntry();
        }

        public virtual async Task<ReminderTableData> ReadRow(GrainId grainId)
        {
            var reminders =
                await Collection.Find(
                    Filter.And(
                        Filter.Eq(x => x.ServiceId, serviceId),
                        Filter.Eq(x => x.GrainHash, grainId.GetUniformHashCode()),
                        Filter.Eq(x => x.GrainId, grainId.ToString())
                    )
                ).ToListAsync();

            return new ReminderTableData(reminders.Select(x => x.ToEntry()));
        }

        public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            var id = ReturnId(serviceId, grainId, reminderName);

            // optimization - we use expression builders over linq
            var deleteResult = await Collection.DeleteOneAsync(
                Filter.And(
                    Filter.Eq(x => x.Id, id),
                    Filter.Eq(x => x.Etag, eTag))
            );
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

            try
            {
                // optimization - we will only upsert the fields which will change on an update to avoid IO cost for static.
                // if there isn't a record matching the filter, a new record will be created with all fields set with the
                // values specified in the filter.
                await Collection.UpdateOneAsync(
                    Filter.And(
                        Filter.Eq(x => x.Id, id),
                        Filter.Eq(x => x.ServiceId, document.ServiceId),
                        Filter.Eq(x => x.GrainId, document.GrainId),
                        Filter.Eq(x => x.ReminderName, document.ReminderName),
                        Filter.Eq(x => x.GrainHash, document.GrainHash)
                    ),
                    Update
                        .Set(x => x.Etag, document.Etag)
                        .Set(x => x.Period, document.Period)
                        .Set(x => x.StartAt, document.StartAt),
                    Upsert
                );
            }
            catch (MongoException ex)
            {
                if (ex.IsDuplicateKey())
                {
                    // in the very unlikely future event that grain hashing (or similar) is computed differently in
                    // future Orleans versions, we will have a duplicate key on the id. We will do a full classic upsert
                    // as a fallback. This will prevent upgrade and timing of multiple releases for MongoDB provider developers
                    await Collection.ReplaceOneAsync(r => r.Id == id, document, UpsertReplace);
                }

                throw;
            }
 
            return entry.ETag = document.Etag;
        }

        private static string ReturnId(string serviceId, GrainId grainId, string reminderName)
        {
            return $"{serviceId}_{grainId}_{reminderName}";
        }
    }
}