using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    ///     Interfaces with a MongoDB database driver.
    /// </summary>
    public class GrainStateMongoDataManager : IJSONStateDataManager
    {
        private readonly IMongoDatabase _database;
        private readonly bool _useJsonFormat;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="connectionString">A database name.</param>
        /// <param name="databaseName">A MongoDB database connection string.</param>
        public GrainStateMongoDataManager(string databaseName, string connectionString, bool useJsonFormat)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
            _useJsonFormat = useJsonFormat;
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
            if (collection == null)
                return Task.CompletedTask;

            var builder = Builders<BsonDocument>.Filter.Eq("key", key);

            return collection.DeleteManyAsync(builder);
        }

        /// <summary>
        ///     Reads a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<BsonDocument> Read(string collectionName, string key)
        {
            var collection = GetCollection(collectionName);
            if (collection == null)
                return null;

            var builder = Builders<BsonDocument>.Filter.Eq("key", key);

            var existing = await collection.Find(builder).FirstOrDefaultAsync();

            if (existing == null)
                return null;

            existing.Remove("_id");
            existing.Remove("key");

            return existing;

            //var strwrtr = new StringWriter();
            
            //var writer = new JsonWriter(strwrtr, new JsonWriterSettings());
            //BsonSerializer.Serialize(writer, existing);

            //// NewtonSoft generates a $type & $id which is incompatible with Mongo. Replacing $ with __
            //return ReverseInvalidValues(strwrtr.ToString());
        }

        /// <summary>
        ///     Writes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task Write(string collectionName, string key, BsonDocument entityData)
        {
            var collection = await GetOrCreateCollection(collectionName);

            var builder = Builders<BsonDocument>.Filter.Eq("key", key);

            var existing = await collection.Find(builder).FirstOrDefaultAsync();

            // NewtonSoft generates a $type & $id which is incompatible with Mongo. Replacing __ with $
            
            //entityData = ReverseInvalidValues(entityData);
            //var doc = BsonSerializer.Deserialize<BsonDocument>(entityData);

            entityData["key"] = key;

            if (existing == null)
            {
                await collection.InsertOneAsync(entityData);
            }
            else
            {
                entityData["_id"] = existing["_id"];
                await collection.ReplaceOneAsync(builder, entityData);
            }
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

        /// <summary>
        ///     Gets a collection from the MongoDB database and creates it if it
        ///     does not already exist.
        /// </summary>
        /// <param name="name">The name of the collection.</param>
        /// <returns></returns>
        private async Task<IMongoCollection<BsonDocument>> GetOrCreateCollection(string name)
        {
            var exists = await CollectionExistsAsync(name);
            var collection = _database.GetCollection<BsonDocument>(name);

            if (exists)
                return collection;
            await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending("key"),
                new CreateIndexOptions());

            return collection;
        }

        public async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            //filter by collection name
            var collections = await _database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
            //check for existence
            return await collections.AnyAsync();
        }
    }
}