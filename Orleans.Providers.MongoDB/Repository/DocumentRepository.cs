namespace Orleans.Providers.MongoDB.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    /// <summary>
    ///     The document repository.
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        #region Static Fields

        /// <summary>
        ///     The database.
        /// </summary>
        private static IMongoDatabase database;

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
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
        /// </param>
        public DocumentRepository(string connectionsString, string databaseName, string mongoCollectionName)
        {
            this.ConnectionString = connectionsString;
            this.DatabaseName = databaseName;
            this.MongoCollectionName = mongoCollectionName;
            IMongoClient client = new MongoClient(this.ConnectionString);
            database = client.GetDatabase(this.DatabaseName);
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

        /// <summary>
        ///     Gets or sets the mongo collection name.
        /// </summary>
        public string MongoCollectionName { get; set; }

        #endregion

        #region Public methods and operators

        /// <summary>
        /// The add document.
        /// </summary>
        /// <param name="document">
        /// The document.
        /// </param>
        public void AddDocument(BsonDocument document)
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new ArgumentException("ConnectionString may not be empty");
            }

            if (string.IsNullOrEmpty(this.MongoCollectionName))
            {
                throw new ArgumentException("MongoCollectionName may not be empty");
            }

            if (document == null)
            {
                throw new ArgumentException("Document may not be empty");
            }

            var collection = database.GetCollection<BsonDocument>(this.MongoCollectionName);
            collection.InsertOne(document);
        }

        /// <summary>
        /// The add document async.
        /// </summary>
        /// <param name="document">
        /// The document.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task AddDocumentAsync(BsonDocument document)
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new ArgumentException("ConnectionString may not be empty");
            }

            if (string.IsNullOrEmpty(this.MongoCollectionName))
            {
                throw new ArgumentException("MongoCollectionName may not be empty");
            }

            if (document == null)
            {
                throw new ArgumentException("Document may not be empty");
            }

            var collection = database.GetCollection<BsonDocument>(this.MongoCollectionName);

            await collection.InsertOneAsync(document);
        }

        #endregion
    }
}
