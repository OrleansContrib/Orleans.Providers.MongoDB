using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.StorageProviders.V2
{
    public interface IMongoStorageCollection
    {
        Task Delete(string key);

        Task<(string Etag, object Value)> Read(string key);

        Task<string> Write(string key, object entityData, string etag);
    }
}