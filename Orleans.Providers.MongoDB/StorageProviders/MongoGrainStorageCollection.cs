using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    internal sealed class MongoGrainStorageCollection : CollectionBase<BsonDocument>
    {
        private const string FieldId = "_id";
        private const string FieldDoc = "_doc";
        private const string FieldEtag = "_etag";
        private readonly string collectionName;
        private readonly IGrainStateSerializer serializer;
        private readonly GrainStorageKeyGenerator keyGenerator;
        private readonly Action<IMongoCollection<BsonDocument>> collectionSetupDelegate;

        public MongoGrainStorageCollection(
            IMongoClient mongoClient,
            string databaseName,
            string collectionName,
            Action<MongoCollectionSettings> collectionConfigurator,
            bool createShardKey,
            IGrainStateSerializer serializer,
            GrainStorageKeyGenerator keyGenerator,
            Action<IMongoCollection<BsonDocument>> collectionSetupDelegate = null
          )
            : base(mongoClient, databaseName, collectionConfigurator, createShardKey)
        {
            this.collectionName = collectionName;
            this.serializer = serializer;
            this.keyGenerator = keyGenerator;
            this.collectionSetupDelegate = collectionSetupDelegate;
        }

        protected override string CollectionName()
        {
            return collectionName;
        }

        public async Task ReadAsync<T>(GrainId grainId, IGrainState<T> grainState)
        {
            var grainKey = keyGenerator(grainId);

            var existing =
                await Collection.Find(Filter.Eq(FieldId, grainKey))
                    .FirstOrDefaultAsync();

            if (existing != null)
            {
                grainState.RecordExists = true;

                if (existing.Contains(FieldDoc))
                {
                    grainState.ETag = existing[FieldEtag].AsString;

                    grainState.State = serializer.Deserialize<T>(existing[FieldDoc]);
                }
                else
                {
                    existing.Remove(FieldId);

                    grainState.State = serializer.Deserialize<T>(existing);
                }
            }
        }

        public async Task WriteAsync<T>(GrainId grainId, IGrainState<T> grainState)
        {
            var grainKey = keyGenerator(grainId);

            var newData = serializer.Serialize(grainState.State);

            var etag = grainState.ETag;

            var newETag = Guid.NewGuid().ToString();

            grainState.RecordExists = true;

            try
            {
                await Collection.UpdateOneAsync(
                    Filter.And(
                        Filter.Eq(FieldId, grainKey),
                        Filter.Eq(FieldEtag, grainState.ETag)),
                    Update
                        .Set(FieldEtag, newETag)
                        .Set(FieldDoc, newData),
                    Upsert);
            }
            catch (MongoException ex)
            {
                if (ex.IsDuplicateKey())
                {
                    await ThrowForOtherEtag(grainKey, etag, ex);

                    var document = new BsonDocument
                    {
                        [FieldId] = grainKey,
                        [FieldEtag] = grainKey,
                        [FieldDoc] = newData
                    };

                    try
                    {
                        await Collection.ReplaceOneAsync(Filter.Eq(FieldId, grainKey), document, UpsertReplace);
                    }
                    catch (MongoException ex2)
                    {
                        if (ex2.IsDuplicateKey())
                        {
                            await ThrowForOtherEtag(grainKey, etag, ex2);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    throw;
                }
            }

            grainState.ETag = newETag;
        }

        public Task ClearAsync<T>(GrainId grainId, IGrainState<T> grainState)
        {
            var grainKey = keyGenerator(grainId);

            grainState.RecordExists = false;

            return Collection.DeleteManyAsync(Filter.Eq(FieldId, grainKey));
        }

        private async Task ThrowForOtherEtag(string key, string etag, Exception ex)
        {
            var existingEtag =
                await Collection.Find(Filter.Eq(FieldId, key))
                    .Project<BsonDocument>(Project.Exclude(FieldDoc)).FirstOrDefaultAsync();

            if (existingEtag != null && existingEtag.Contains(FieldEtag))
            {
                throw new InconsistentStateException(existingEtag[FieldEtag].AsString, etag, ex);
            }
        }

        protected override void SetupCollection(IMongoCollection<BsonDocument> collection)
        {
            collectionSetupDelegate?.Invoke(collection);
        }
  }
}
