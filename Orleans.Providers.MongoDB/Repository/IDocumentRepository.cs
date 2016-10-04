namespace Orleans.Providers.MongoDB.Repository
{
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    public interface IDocumentRepository
    {
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
        Task<DeleteResult> DeleteDocumentAsync(string mongoCollectionName, string keyName, string key);
        Task SaveDocumentAsync(string mongoCollectionName, string keyName, string key, BsonDocument document);      
        Task<BsonDocument> FindDocumentAsync(string mongoCollectionName, string keyName, string key);

    }
}