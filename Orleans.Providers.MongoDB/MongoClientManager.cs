using System.Collections.Concurrent;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB
{
    /// <summary>
    ///     Mongo connection manager. Prevents connections being created everytime.
    ///     Only one connection will be created per connectionstring
    /// </summary>
    public static class MongoClientManager
    {
        private static readonly ConcurrentDictionary<string, IMongoClient> Instances =
            new ConcurrentDictionary<string, IMongoClient>();

        public static IMongoClient Instance(string connectionString)
        {
            return Instances.GetOrAdd(connectionString, cs => new MongoClient(cs));
        }
    }
}