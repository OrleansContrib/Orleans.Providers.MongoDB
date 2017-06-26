namespace Orleans.Providers.MongoDB.StorageProviders
{
    using System;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using Newtonsoft.Json;
    using System.IO;
    using System.Text;
    using Orleans.Providers.MongoDB.StorageProviders.Serializing;

    /// <summary>
    /// A MongoDB storage provider.
    /// </summary>
    /// <remarks>
    /// The storage provider should be included in a deployment by adding this line to the Orleans server configuration file:
    /// 
    ///     <Provider Type="Orleans.Providers.MongoDB.StorageProviders.MongoDBStorage" Name="MongoDBStore" Database="db-name" ConnectionString="mongodb://YOURHOSTNAME:27017/" />
    ///                                                                                 or
    ///     <Provider Type="Orleans.Providers.MongoDB.StorageProviders.MongoDBStorage" Name="MongoDBStore" Database="" ConnectionString="mongodb://YOURHOSTNAME:27017/db-name" />
    /// and this line to any grain that uses it:
    /// 
    ///     [StorageProvider(ProviderName = "MongoDBStore")]
    /// 
    /// The name 'MongoDBStore' is an arbitrary choice.
    /// </remarks>
    public class MongoDBStorage : BaseJSONStorageProvider
    {
        /// <summary>
        /// Database connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;
            this.ConnectionString = config.Properties["ConnectionString"];

            if (!config.Properties.ContainsKey("Database") || string.IsNullOrEmpty(config.Properties["Database"]))
            {
                this.Database = MongoUrl.Create(this.ConnectionString).DatabaseName;
            }
            else
            {
                this.Database = config.Properties["Database"];
            }

            if (string.IsNullOrWhiteSpace(this.ConnectionString)) throw new ArgumentException("ConnectionString property not set");
            if (string.IsNullOrWhiteSpace(this.Database)) throw new ArgumentException("Database property not set");
            this.DataManager = new GrainStateMongoDataManager(this.Database, this.ConnectionString);
            return base.Init(name, providerRuntime, config);
        }
    }

    /// <summary>
    /// Interfaces with a MongoDB database driver.
    /// </summary>
    internal class GrainStateMongoDataManager : IJSONStateDataManager
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">A database name.</param>
        /// <param name="databaseName">A MongoDB database connection string.</param>
        public GrainStateMongoDataManager(string databaseName, string connectionString)
        {
            MongoClient client = new MongoClient(connectionString);
            this._database = client.GetDatabase(databaseName);
        }

        /// <summary>
        /// Deletes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task Delete(string collectionName, string key)
        {
            var collection = this.GetCollection(collectionName);
            if (collection == null)
                return TaskDone.Done;

            var builder = Builders<BsonDocument>.Filter.Eq("key", key);

            return collection.DeleteManyAsync(builder);
        }

        /// <summary>
        /// Reads a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<string> Read(string collectionName, string key)
        {
            var collection = this.GetCollection(collectionName);
            if (collection == null)
                return null;

            var builder = Builders<BsonDocument>.Filter.Eq("key", key);

            var existing = await collection.Find(builder).FirstOrDefaultAsync();

            if (existing == null)
                return null;

            existing.Remove("_id");
            existing.Remove("key");

            var strwrtr = new System.IO.StringWriter();
            var writer = new global::MongoDB.Bson.IO.JsonWriter(strwrtr, new global::MongoDB.Bson.IO.JsonWriterSettings());
            global::MongoDB.Bson.Serialization.BsonSerializer.Serialize(writer, existing);

            // NewtonSoft generates a $type & $id which is incompatible with Mongo. Replacing $ with __
            return this.ReverseInvalidValues(strwrtr.ToString());
        }

        /// <summary>
        /// Writes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task Write(string collectionName, string key, string entityData)
        {
            var collection = await this.GetOrCreateCollection(collectionName);

            var builder = Builders<BsonDocument>.Filter.Eq("key", key);

            var existing = await collection.Find(builder).FirstOrDefaultAsync();

            // NewtonSoft generates a $type & $id which is incompatible with Mongo. Replacing __ with $
            entityData = this.ReverseInvalidValues(entityData);
            var doc = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(entityData);

            doc["key"] = key;

            if (existing == null)
            {
                await collection.InsertOneAsync(doc);
            }
            else
            {
                doc["_id"] = existing["_id"];
                await collection.ReplaceOneAsync(builder, doc);
            }
        }

        /// <summary>
        /// NewtonSoft generates a $type & $id which is incompatible with Mongo. Replacing $ with __
        /// </summary>
        /// <param name="entityData"></param>
        /// <returns></returns>
        private string ReverseInvalidValues(string entityData)
        {
            StringBuilder sb = new StringBuilder();
            CustomJsonWriter writer = new CustomJsonWriter(new StringWriter(sb));
            JsonTextReader reader = new JsonTextReader(new StringReader(entityData));

            int n = 0;

            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);

                    switch (reader.TokenType)
                    {
                        case JsonToken.PropertyName:

                            if (reader.Value.ToString() == "$type")
                            {
                                writer.WritePropertyName("__type");
                            }
                            else if (reader.Value.ToString() == "__type")
                            {
                                writer.WritePropertyName("$type");
                            }
                            else if (reader.Value.ToString() == "$id")
                            {
                                writer.WritePropertyName("__id");
                            }
                            else if (reader.Value.ToString() == "__id")
                            {
                                writer.WritePropertyName("$id");
                            }
                            else if (reader.Value.ToString() == "$type")
                            {
                                writer.WritePropertyName("__type");
                            }
                            else if (reader.Value.ToString() == "__type")
                            {
                                writer.WritePropertyName("$type");
                            }
                            else if (reader.Value.ToString() == "$values")
                            {
                                writer.WritePropertyName("__values");
                            }
                            else if (reader.Value.ToString() == "__values")
                            {
                                writer.WritePropertyName("$values");
                            }
                            else
                            {
                                writer.WritePropertyName(reader.Value.ToString());
                            }

                            break;
                        case JsonToken.None:
                            break;
                        case JsonToken.StartConstructor:
                            writer.WriteStartConstructor(reader.Value.ToString());
                            break;
                        case JsonToken.Comment:
                            writer.WriteComment(reader.Value.ToString());
                            break;
                        case JsonToken.Raw:
                            writer.WriteRaw(reader.Value.ToString());
                            break;
                        case JsonToken.Integer:
                        case JsonToken.Float:
                        case JsonToken.String:
                        case JsonToken.Boolean:
                        case JsonToken.Date:
                        case JsonToken.Bytes:
                            writer.WriteValue(reader.Value);
                            break;
                        case JsonToken.Null:
                            writer.WriteNull();
                            break;
                        case JsonToken.Undefined:
                            writer.WriteUndefined();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Token: {0}", reader.TokenType);

                    switch (reader.TokenType)
                    {
                        case JsonToken.None:
                            break;
                        case JsonToken.StartObject:
                            writer.WriteStartObject();
                            break;
                        case JsonToken.StartArray:
                            writer.WriteStartArray();
                            break;
                        case JsonToken.Null:
                            writer.WriteNull();
                            break;
                        case JsonToken.Undefined:
                            writer.WriteUndefined();
                            break;
                        case JsonToken.EndObject:
                            writer.WriteEndObject();
                            break;
                        case JsonToken.EndArray:
                            writer.WriteEndArray();
                            break;
                        case JsonToken.EndConstructor:
                            writer.WriteEndConstructor();
                            break;
                        default:
                            break;
                    }

                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a collection from the MongoDB database.
        /// </summary>
        /// <param name="name">The name of the collection.</param>
        /// <returns></returns>
        private IMongoCollection<BsonDocument> GetCollection(string name)
        {
            return this._database.GetCollection<BsonDocument>(name);
        }

        /// <summary>
        /// Gets a collection from the MongoDB database and creates it if it
        /// does not already exist.
        /// </summary>
        /// <param name="name">The name of the collection.</param>
        /// <returns></returns>
        private async Task<IMongoCollection<BsonDocument>> GetOrCreateCollection(string name)
        {
            bool exists = await this.CollectionExistsAsync(name);
            var collection = this._database.GetCollection<BsonDocument>(name);

            if (exists)
            {
                return collection;
            }
            else
            {
                await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending("key"), new CreateIndexOptions());
            }

            return collection;
        }

        public async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            //filter by collection name
            var collections = await this._database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            //check for existence
            return await collections.AnyAsync();
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
        }

        private readonly IMongoDatabase _database;

    }
}