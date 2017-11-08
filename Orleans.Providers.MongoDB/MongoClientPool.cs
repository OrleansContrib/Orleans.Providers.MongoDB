using System.Collections.Concurrent;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB
{
    public static class MongoClientPool
    {
        private static readonly ConcurrentDictionary<string, IMongoClient> Instances =
            new ConcurrentDictionary<string, IMongoClient>();

        public static IMongoClient Instance(string connectionString)
        {
            var urlBuilder = new MongoUrlBuilder(connectionString)
            {
                DatabaseName = null
            };

            var sanitizedConnectionString = urlBuilder.ToString();

            return Instances.GetOrAdd(sanitizedConnectionString, cs => new MongoClient(cs));
        }
    }
}