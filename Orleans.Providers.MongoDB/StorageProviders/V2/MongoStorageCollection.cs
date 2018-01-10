using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Storage;
using System;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.StorageProviders.V2
{
    public sealed class MongoStorageCollection<T> : IMongoStorageCollection
    {
        private static readonly UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };
        private static readonly SortDefinitionBuilder<StorageDocument<T>> Sort = Builders<StorageDocument<T>>.Sort;
        private static readonly UpdateDefinitionBuilder<StorageDocument<T>> Update = Builders<StorageDocument<T>>.Update;
        private static readonly FilterDefinitionBuilder<StorageDocument<T>> Filter = Builders<StorageDocument<T>>.Filter;
        private static readonly IndexKeysDefinitionBuilder<StorageDocument<T>> Index = Builders<StorageDocument<T>>.IndexKeys;
        private static readonly ProjectionDefinitionBuilder<StorageDocument<T>> Project = Builders<StorageDocument<T>>.Projection;
        private readonly IMongoCollection<StorageDocument<T>> collection;

        public MongoStorageCollection(IMongoDatabase database, string collectionName)
        {
            collection = database.GetCollection<StorageDocument<T>>(collectionName);
        }

        public Task Delete(string key)
        {
            return collection.DeleteManyAsync(x => x.Key == key);
        }

        public async Task<(string Etag, object Value)> Read(string key)
        {
            var existing =
                await collection.Find(x => x.Key == key)
                    .FirstOrDefaultAsync();

            if (existing != null)
            {
                return (existing.Etag, existing.State);
            }

            return (null, null);
        }

        public async Task<string> Write(string key, object entityData, string etag)
        {
            var newETag = Guid.NewGuid().ToString();

            try
            {
                await collection.UpdateOneAsync(x => x.Key == key && x.Etag == etag,
                    Update
                        .Set(x => x.Etag, newETag)
                        .Set(x => x.State, (T)entityData),
                    Upsert);
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    var existingEtag =
                        await collection.Find(x => x.Key == key)
                            .Project<BsonDocument>(Project.Exclude("State")).FirstOrDefaultAsync();

                    if (existingEtag != null && existingEtag.Contains("Etag"))
                    {
                        throw new InconsistentStateException(existingEtag["Etag"].AsString, etag, ex);
                    }
                }

                throw;
            }

            return newETag;
        }
    }
}
