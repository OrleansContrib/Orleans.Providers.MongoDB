using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.Utils
{
    public interface IMongoClientFactory
    {
        IMongoClient Create(string name);
    }
}
