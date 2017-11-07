using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    ///     Interfaces with a MongoDB database driver.
    /// </summary>
    public class GrainStateMongoDataManager : IJSONStateDataManager
    {
        private static UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };
        private readonly IMongoDatabase _database;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="connectionString">A database name.</param>
        /// <param name="databaseName">A MongoDB database connection string.</param>
        public GrainStateMongoDataManager(string databaseName, string connectionString)
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
            
            return collection.DeleteManyAsync(ById(key));
        }

        /// <summary>
        ///     Reads a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<JObject> Read(string collectionName, string key)
        {
            var collection = GetCollection(collectionName);
            
            var existing = await collection.Find(ById(key)).FirstOrDefaultAsync();
            
            existing?.Remove("_id");

            return existing?.ToJToken();
        }

        /// <summary>
        ///     Writes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task Write(string collectionName, string key, JObject entityData)
        {
            var collection = GetCollection(collectionName);
            
            var bsonDocument = entityData.ToBson();

            bsonDocument["_id"] = key;

            return collection.ReplaceOneAsync(ById(key), entityData.ToBson(), Upsert);
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