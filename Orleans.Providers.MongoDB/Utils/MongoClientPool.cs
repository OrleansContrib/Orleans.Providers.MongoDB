using System.Collections.Concurrent;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.Utils
{
    internal static class MongoClientPool
    {
        private static readonly ConcurrentDictionary<string, IMongoClient> Instances =
            new ConcurrentDictionary<string, IMongoClient>();

        public static IMongoClient Instance(string connectionString)
        {
            return Instances.GetOrAdd(connectionString, cs => new MongoClient(cs));
        }
    }
}