using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public interface IJSONStateDataManager : IDisposable
    {
        Task Delete(string collectionName, string key);
        
        Task<(string Etag, JObject Value)> Read(string collectionName, string key);
        
        Task<string> Write(string collectionName, string key, JObject entityData, string etag);
    }
}