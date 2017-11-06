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
            var urlBuilder = new MongoUrlBuilder(connectionString)
            {
                DatabaseName = null
            };

            var sanitizedConnectionString = urlBuilder.ToString();

            return Instances.GetOrAdd(sanitizedConnectionString, cs => new MongoClient(cs));
        }
    }
}