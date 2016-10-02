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

        
        Task<DeleteResult> DeleteDocumentAsync(string mongoCollectionName, string keyName, string key);


        Task SaveDocumentAsync(string mongoCollectionName, string keyName, string key, BsonDocument document);
        
        Task<BsonDocument> FindDocumentAsync(string mongoCollectionName, string keyName, string key);

        #endregion
    }
}