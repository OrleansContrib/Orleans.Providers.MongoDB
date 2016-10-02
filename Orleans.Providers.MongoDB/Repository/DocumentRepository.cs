namespace Orleans.Providers.MongoDB.Repository
{
    #region Using

    using System;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    #endregion

    /// <summary>
    ///     The document repository.
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        #region Static Fields

        /// <summary>
        ///     The database.
        /// </summary>
        protected static IMongoDatabase Database;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentRepository"/> class.
        /// </summary>
        /// <param name="connectionsString">
        /// The connections string.
        /// </param>
        /// <param name="databaseName">
        /// The database name.
        /// </param>
        public DocumentRepository(string connectionsString, string databaseName)
        {
            this.ConnectionString = connectionsString;
            this.DatabaseName = databaseName;
            IMongoClient client = new MongoClient(this.ConnectionString);
            Database = client.GetDatabase(this.DatabaseName);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Gets or sets the database.
        /// </summary>
        public string DatabaseName { get; set; }

        #endregion

        #region Public methods and operators

        /// <summary>
        /// The delete document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
        /// </param>
        /// <param name="keyName">
        /// The key name.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        /// <exception cref="Exception">
        /// </exception>
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

        /// <summary>
        /// The find document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
        /// </param>
        /// <param name="keyName">
        /// The key name.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
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

        /// <summary>
        /// The save document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
        /// </param>
        /// <param name="keyName">
        /// The key name.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="document">
        /// The document.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
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

        #endregion

        #region Other Methods

        /// <summary>
        /// The return or create collection.
        /// </summary>
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
        /// </param>
        /// <returns>
        /// The <see cref="IMongoCollection"/>.
        /// </returns>
        protected IMongoCollection<BsonDocument> ReturnOrCreateCollection(string mongoCollectionName)
        {
            var collection = Database.GetCollection<BsonDocument>(mongoCollectionName);
            if (collection != null) return collection;
            Database.CreateCollection(mongoCollectionName);
            collection = Database.GetCollection<BsonDocument>(mongoCollectionName);
            return collection;
        }

        #endregion
    }
}