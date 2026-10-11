using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Serialization.Serializers;
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
        private readonly IActivatorProvider activatorProvider;
        private readonly GrainStorageKeyGenerator keyGenerator;

        public MongoGrainStorageCollection(
            IMongoClient mongoClient,
            string databaseName,
            string collectionName,
            Action<MongoCollectionSettings> collectionConfigurator,
            bool createShardKey,
            IGrainStateSerializer serializer,
            IActivatorProvider activatorProvider,
            GrainStorageKeyGenerator keyGenerator)
            : base(mongoClient, databaseName, collectionConfigurator, createShardKey)
        {
            this.collectionName = collectionName;
            this.serializer = serializer;
            this.activatorProvider = activatorProvider;
            this.keyGenerator = keyGenerator;
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
            else
            {
                ResetState(grainState);
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
                        [FieldEtag] = newETag,
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

        public async Task ClearAsync<T>(GrainId grainId, IGrainState<T> grainState)
        {
            var grainKey = keyGenerator(grainId);

            var result = await Collection.DeleteOneAsync(
                Filter.And(
                    Filter.Eq(FieldId, grainKey),
                    Filter.Eq(FieldEtag, grainState.ETag)));

            if (result.DeletedCount == 0)
            {
                await ThrowForOtherEtag(grainKey, grainState.ETag, null);

                // Documents from before the etag was introduced have no etag to check, so they are deleted like they are overwritten.
                await Collection.DeleteOneAsync(
                    Filter.And(
                        Filter.Eq(FieldId, grainKey),
                        Filter.Exists(FieldEtag, false)));
            }

            ResetState(grainState);
        }

        private void ResetState<T>(IGrainState<T> grainState)
        {
            grainState.ETag = null;
            grainState.RecordExists = false;
            grainState.State = activatorProvider.GetActivator<T>().Create();
        }

        private async Task ThrowForOtherEtag(string key, string etag, Exception ex)
        {
            var existingEtag =
                await Collection.Find(Filter.Eq(FieldId, key))
                    .Project<BsonDocument>(Project.Exclude(FieldDoc)).FirstOrDefaultAsync();

            if (existingEtag != null && existingEtag.Contains(FieldEtag))
            {
                var storedEtag = existingEtag[FieldEtag].AsString;

                var message = ex?.Message ?? $"Version conflict: stored ETag {storedEtag} does not match current ETag {etag}.";

                throw new InconsistentStateException(message, storedEtag, etag, ex);
            }
        }
    }
}
