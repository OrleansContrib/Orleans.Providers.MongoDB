using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.Repository
{
    /// <summary>
    ///     The DocumentRepository interface.
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        ///     Gets or sets the connection string.
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        ///     Gets or sets the database name.
        /// </summary>
        string DatabaseName { get; set; }

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
        void AddDocuments(
            List<BsonDocument> documents,
            string mongoCollectionName,
            bool isOrdered = true,
            bool bypassDocumentValidation = false);

        /// <summary>
        ///     Add documents async.
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
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task AddDocumentsAsync(
            List<BsonDocument> documents,
            string mongoCollectionName,
            bool isOrdered = true,
            bool bypassDocumentValidation = false);

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
        Task BulkWriteAsync(WriteModel<BsonDocument>[] documents, string mongoCollectionName);

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
        Task<DeleteResult> DeleteDocumentAsync(string mongoCollectionName, string keyName, string key);

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
        Task<BsonDocument> FindDocumentAsync<KeyType>(string mongoCollectionName, string keyName, KeyType key);

        /// <summary>
        ///     Return all async.
        /// </summary>
        /// <param name="mongoCollectionName">
        ///     The mongo collection name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task<List<BsonDocument>> ReturnAllAsync(string mongoCollectionName);

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
        Task SaveDocumentAsync(string mongoCollectionName, string keyName, string key, BsonDocument document);

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
        Task UpsertDocumentsAsync(List<BsonDocument> documents, string lookupFieldName, string mongoCollectionName);
    }
}