namespace Orleans.Providers.MongoDB.Repository
{
    #region Using

    using System.Threading.Tasks;

    using global::MongoDB.Bson;

    #endregion

    /// <summary>
    ///     The DocumentRepository interface.
    /// </summary>
    public interface IDocumentRepository
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the connection string.
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        ///     Gets or sets the database.
        /// </summary>
        string DatabaseName { get; set; }

        /// <summary>
        ///     Gets or sets the mongo collection name.
        /// </summary>
        string MongoCollectionName { get; set; }

        #endregion

        #region Public methods and operators

        /// <summary>
        /// The add document.
        /// </summary>
        /// <param name="document">
        /// The document.
        /// </param>
        void AddDocument(BsonDocument document);

        /// <summary>
        /// The add document async.
        /// </summary>
        /// <param name="document">
        /// The document.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task AddDocumentAsync(BsonDocument document);

        #endregion
    }
}