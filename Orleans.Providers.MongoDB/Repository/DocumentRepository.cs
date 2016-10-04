namespace Orleans.Providers.MongoDB.Repository
{
    using System;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    public class DocumentRepository : IDocumentRepository
    {
        protected static IMongoDatabase Database;

        public DocumentRepository(string connectionsString, string databaseName)
        {
            this.ConnectionString = connectionsString;
            this.DatabaseName = databaseName;
            IMongoClient client = new MongoClient(this.ConnectionString);
            Database = client.GetDatabase(this.DatabaseName);
        }

        public string ConnectionString { get; set; }

        public string DatabaseName { get; set; }

        public async Task<DeleteResult> DeleteDocumentAsync(string mongoCollectionName, string keyName, string key)
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new ArgumentException("ConnectionString may not be empty");
            }

            if (string.IsNullOrEmpty(mongoCollectionName))
            {
                throw new ArgumentException("MongoCollectionName may not be empty");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key may not be empty");
            }

            var collection = this.ReturnOrCreateCollection(mongoCollectionName);
            if (collection == null)
            {
                throw new Exception("Invalid Collection");
            }

            var builder = Builders<BsonDocument>.Filter.Eq(keyName, key);

            return await collection.DeleteManyAsync(builder);
        }

        public async Task<BsonDocument> FindDocumentAsync(string mongoCollectionName, string keyName, string key)
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new ArgumentException("ConnectionString may not be empty");
            }

            if (string.IsNullOrEmpty(mongoCollectionName))
            {
                throw new ArgumentException("MongoCollectionName may not be empty");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key may not be empty");
            }

            var collection = this.ReturnOrCreateCollection(mongoCollectionName);
            if (collection == null) return null;

            var builder = Builders<BsonDocument>.Filter.Eq(keyName, key);

            var result = await collection.Find(builder).FirstOrDefaultAsync();

            if (result == null) return null;

            // result.Remove("_id");
            // result.Remove("key");
            return result;
        }

        public async Task SaveDocumentAsync(
            string mongoCollectionName,
            string keyName,
            string key,
            BsonDocument document)
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new ArgumentException("ConnectionString may not be empty");
            }

            if (string.IsNullOrEmpty(mongoCollectionName))
            {
                throw new ArgumentException("MongoCollectionName may not be empty");
            }

            if (document == null)
            {
                throw new ArgumentException("Document may not be empty");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key may not be empty");
            }

            var collection = this.ReturnOrCreateCollection(mongoCollectionName);

            var builder = Builders<BsonDocument>.Filter.Eq(keyName, key);

            var existing = await collection.Find(builder).FirstOrDefaultAsync();

            if (existing == null)
            {
                await collection.InsertOneAsync(document);
            }
            else
            {
                document["_id"] = existing["_id"];
                await collection.ReplaceOneAsync(builder, document);
            }
        }

        protected IMongoCollection<BsonDocument> ReturnOrCreateCollection(string mongoCollectionName)
        {
            var collection = Database.GetCollection<BsonDocument>(mongoCollectionName);
            if (collection != null) return collection;
            Database.CreateCollection(mongoCollectionName);
            collection = Database.GetCollection<BsonDocument>(mongoCollectionName);
            return collection;
        }
    }
}