namespace Orleans.Providers.MongoDB.Repository
{
    #region Using

    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

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
        
        #endregion

        #region Public methods and operators

        /// <summary>
        /// The delete document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<DeleteResult> DeleteDocumentAsync(string mongoCollectionName, string key);

        /// <summary>
        /// The save document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
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
        Task SaveDocumentAsync(string mongoCollectionName, string key, BsonDocument document);

        /// <summary>
        /// The find document async.
        /// </summary>
        /// <param name="mongoCollectionName">
        /// The mongo collection name.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<BsonDocument> FindDocumentAsync(string mongoCollectionName, string key);

        #endregion
    }
}