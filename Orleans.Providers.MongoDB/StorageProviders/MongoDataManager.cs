using System.Threading.Tasks;
using System;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    ///     Interfaces with a MongoDB database driver.
    /// </summary>
    public class MongoDataManager : IJSONStateDataManager
    {
        private static readonly UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };
        private static readonly FilterDefinitionBuilder<BsonDocument> Filter = Builders<BsonDocument>.Filter;
        private static readonly UpdateDefinitionBuilder<BsonDocument> Update = Builders<BsonDocument>.Update;
        private static readonly ProjectionDefinitionBuilder<BsonDocument> Projection = Builders<BsonDocument>.Projection;
        private const string FieldId = "_id";
        private const string FieldDoc = "_doc";
        private const string FieldEtag = "_etag";
        private readonly IMongoDatabase _database;

        public IMongoDatabase Database
        {
            get { return _database; }
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="connectionString">A database name.</param>
        /// <param name="databaseName">A MongoDB database connection string.</param>
        public MongoDataManager(string databaseName, string connectionString)
        {
            var client = MongoClientManager.Instance(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        /// <summary>
        ///     Deletes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task Delete(string collectionName, string key)
        {
            var collection = GetCollection(collectionName);
            
            return collection.DeleteManyAsync(Filter.Eq(FieldId, key));
        }

        /// <summary>
        ///     Reads a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<(string Etag, JObject Value)> Read(string collectionName, string key)
        {
            var collection = GetCollection(collectionName);
            
            var existing = 
                await collection.Find(Filter.Eq(FieldId, key))
                    .FirstOrDefaultAsync();

            if (existing != null)
            {
                if (existing.Contains(FieldDoc))
                {
                    return (existing[FieldEtag].AsString, existing[FieldDoc].AsBsonDocument.ToJToken());
                }
                else
                {
                    existing.Remove(FieldId);

                    return (null, existing.ToJToken());
                }
            }

            return (null, null);
        }

        /// <summary>
        ///     Writes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<string> Write(string collectionName, string key, JObject entityData, string etag)
        {
            var collection = GetCollection(collectionName);

            var newData = entityData.ToBson();
            var newETag = Guid.NewGuid().ToString();

            try
            {
                await collection.UpdateOneAsync(
                    Filter.And(
                        Filter.Eq(FieldId, key),
                        Filter.Eq(FieldEtag, etag)
                    ),
                    Update
                        .Set(FieldEtag, newETag)
                        .Set(FieldDoc, newData),
                    Upsert);
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    await ThrowForOtherEtag(collection, key, etag, ex);

                    var document = new BsonDocument
                    {
                        [FieldId] = key,
                        [FieldEtag] = etag,
                        [FieldDoc] = newData
                    };

                    try
                    {
                        await collection.ReplaceOneAsync(Filter.Eq(FieldId, key), document, Upsert);
                    }
                    catch (MongoWriteException ex2)
                    {
                        if (ex2.WriteError.Category == ServerErrorCategory.DuplicateKey)
                        {
                            await ThrowForOtherEtag(collection, key, etag, ex2);
                        }
                    }
                }
                else
                {
                    throw;
                }
            }

            return newETag;
        }

        private static async Task ThrowForOtherEtag(IMongoCollection<BsonDocument> collection, string key, string etag,  MongoWriteException ex)
        {
            var existingEtag =
                await collection.Find(Filter.Eq(FieldId, key))
                    .Project<BsonDocument>(Projection.Exclude(FieldDoc)).FirstOrDefaultAsync();

            if (existingEtag != null && existingEtag.Contains(FieldEtag))
            {
                throw new InconsistentStateException(existingEtag[FieldEtag].AsString, etag, ex);
            }
        }

        private static FilterDefinition<BsonDocument> ById(string key)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", key);
        }

        /// <summary>
        ///     Clean up.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///     Gets a collection from the MongoDB database.
        /// </summary>
        /// <param name="name">The name of the collection.</param>
        /// <returns></returns>
        private IMongoCollection<BsonDocument> GetCollection(string name)
        {
            return _database.GetCollection<BsonDocument>(name);
        }
    }
}