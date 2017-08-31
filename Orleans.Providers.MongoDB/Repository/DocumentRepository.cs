using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository.ConnectionManager;

namespace Orleans.Providers.MongoDB.Repository
{
    public class DocumentRepository : IDocumentRepository
    {
        #region Static Fields

        /// <summary>
        ///     Mongo Database.
        /// </summary>
        protected static IMongoDatabase Database;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="DocumentRepository" /> class.
        /// </summary>
        /// <param name="connectionsString">
        ///     The connections string.
        /// </param>
        /// <param name="databaseName">
        ///     The database name.
        /// </param>
        public DocumentRepository(string connectionsString, string databaseName)
        {
            if (string.IsNullOrEmpty(connectionsString))
                throw new ArgumentException("ConnectionString May Not be Empty");

            ConnectionString = connectionsString;
            DatabaseName = databaseName;

            if (string.IsNullOrEmpty(databaseName))
                DatabaseName = MongoUrl.Create(connectionsString).DatabaseName;

            var client = MongoConnectionManager.Instance(connectionsString, databaseName);
            Database = client.GetDatabase(DatabaseName);
        }

        #endregion

        #region Other Methods

        /// <summary>
        ///     Return or create collection.
        /// </summary>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="IMongoCollection" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        protected IMongoCollection<BsonDocument> ReturnOrCreateCollection(string mongoCollectionName)
        {
            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            // A collection is created if one isn't found
            var collection = Database.GetCollection<BsonDocument>(mongoCollectionName);

            return collection;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Gets or sets the database name.
        /// </summary>
        public string DatabaseName { get; set; }

        #endregion

        #region Public methods and operators

        /// <summary>
        ///     Clear collection.
        /// </summary>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task ClearCollection(string mongoCollectionName)
        {
            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            var collection = Database.GetCollection<BsonDocument>(mongoCollectionName);
            await collection.DeleteManyAsync(new BsonDocument()).ConfigureAwait(false);
        }

        /// <summary>
        ///     Collection exists async.
        /// </summary>
        /// <param name="collectionName">
        ///     The collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task<bool> CollectionExistsAsync(string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException("CollectionName may not be empty");

            var filter = new BsonDocument("name", collectionName);

            var collections = await Database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});

            // check for existence
            return (await collections.ToListAsync()).Any();
        }


        /// <summary>
        ///     Delete document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <param name="keyName">
        ///     The key name.
        /// </param>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        /// <exception cref="Exception">
        /// </exception>
        public async Task<DeleteResult> DeleteDocumentAsync(string mongoCollectionName, string keyName, string key)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentException("ConnectionString may not be empty");

            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key may not be empty");

            var collection = ReturnOrCreateCollection(mongoCollectionName);
            if (collection == null)
                throw new Exception("Invalid Collection");

            var builder = Builders<BsonDocument>.Filter.Eq(keyName, key);

            return await collection.DeleteManyAsync(builder);
        }

        /// <summary>
        ///     Find document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <param name="keyName">
        ///     The key name.
        /// </param>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task<BsonDocument> FindDocumentAsync<KeyType>(string mongoCollectionName, string keyName,
            KeyType key)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentException("ConnectionString may not be empty");

            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            //if (string.IsNullOrEmpty(key))
            //{
            //    Logger.Error("Key may not be empty");
            //    throw new ArgumentException("Key may not be empty");
            //}

            var collection = ReturnOrCreateCollection(mongoCollectionName);
            if (collection == null)
                return null;

            var builder = Builders<BsonDocument>.Filter.Eq(keyName, key);

            var result = await collection.Find(builder).FirstOrDefaultAsync();

            if (result == null)
                return null;

            return result;
        }

        /// <summary>
        ///     Return all async.
        /// </summary>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task<List<BsonDocument>> ReturnAllAsync(string mongoCollectionName)
        {
            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            var collection = Database.GetCollection<BsonDocument>(mongoCollectionName);
            return await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        }

        /// <summary>
        ///     Save document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <param name="keyName">
        ///     The key name.
        /// </param>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="document">
        ///     The document.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task SaveDocumentAsync(
            string mongoCollectionName,
            string keyName,
            string key,
            BsonDocument document)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentException("ConnectionString may not be empty");

            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            if (document == null)
                throw new ArgumentException("Document may not be empty");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key may not be empty");

            var collection = ReturnOrCreateCollection(mongoCollectionName);

            var builder = Builders<BsonDocument>.Filter.Eq(keyName, key);

            var existing = await collection.Find(builder).FirstOrDefaultAsync();

            if (existing == null)
                await collection.InsertOneAsync(document);
            else
                await collection.ReplaceOneAsync(builder, document);
        }

        /// <summary>
        ///     Add documents.
        /// </summary>
        /// <param name="documents">
        ///     The documents.
        /// </param>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <param name="isOrdered">
        ///     The is ordered.
        /// </param>
        /// <param name="bypassDocumentValidation">
        ///     The bypass document validation.
        /// </param>
        /// <exception cref="ArgumentException">
        /// </exception>
        public void AddDocuments(List<BsonDocument> documents, string mongoCollectionName, bool isOrdered = true,
            bool bypassDocumentValidation = false)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentException("ConnectionString may not be empty");

            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            if (documents == null || documents.Count == 0)
                throw new ArgumentException("Document may not be empty");

            var collection = ReturnOrCreateCollection(mongoCollectionName);
            collection.InsertMany(documents,
                new InsertManyOptions {BypassDocumentValidation = bypassDocumentValidation, IsOrdered = isOrdered});
        }

        /// <summary>
        ///     Add documents async.
        /// </summary>
        /// <param name="documents">
        ///     The documents.
        /// </param>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task AddDocumentsAsync(List<BsonDocument> documents, string mongoCollectionName,
            bool isOrdered = true, bool bypassDocumentValidation = false)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentException("ConnectionString may not be empty");

            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            if (documents == null)
                throw new ArgumentException("Document may not be empty");

            var collection = ReturnOrCreateCollection(mongoCollectionName);

            await collection.InsertManyAsync(documents,
                new InsertManyOptions {BypassDocumentValidation = bypassDocumentValidation, IsOrdered = isOrdered});
        }

        /// <summary>
        ///     Upsert documents async.
        /// </summary>
        /// <param name="documents">
        ///     The documents.
        /// </param>
        /// <param name="lookupFieldName">
        ///     The lookup field name.
        /// </param>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task UpsertDocumentsAsync(List<BsonDocument> documents, string lookupFieldName,
            string mongoCollectionName)
        {
            var documentsWriteModel = new WriteModel<BsonDocument>[documents.Count];
            FilterDefinition<BsonDocument> filter = null;

            BsonDocument document = null;

            for (var i = 0; i <= documents.Count - 1; i++)
            {
                document = documents[i];
                filter = Builders<BsonDocument>.Filter.Eq(lookupFieldName,
                    document[lookupFieldName].AsDouble);

                documentsWriteModel[i] = new ReplaceOneModel<BsonDocument>(filter, document) {IsUpsert = true};
            }

            await BulkWriteAsync(documentsWriteModel, mongoCollectionName);
        }

        /// <summary>
        ///     Bulk write async.
        /// </summary>
        /// <param name="documents">
        ///     The documents.
        /// </param>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task BulkWriteAsync(WriteModel<BsonDocument>[] documents, string mongoCollectionName)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentException("ConnectionString may not be empty");

            if (string.IsNullOrEmpty(mongoCollectionName))
                throw new ArgumentException("MongoCollectionName may not be empty");

            if (documents == null)
                throw new ArgumentException("Document may not be empty");

            var collection = ReturnOrCreateCollection(mongoCollectionName);

            var result = await collection.BulkWriteAsync(documents, new BulkWriteOptions {IsOrdered = false});
        }

        #endregion
    }
}