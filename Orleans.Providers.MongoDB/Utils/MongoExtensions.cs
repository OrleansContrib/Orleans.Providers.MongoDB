using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;

namespace Orleans.Providers.MongoDB.Utils
{
    public static class MongoExtensions
    {
        public static bool IsDuplicateKey(this MongoException ex)
        {
            if (ex is MongoCommandException c && c.Code == 11000)
            {
                return true;
            }
            if (ex is MongoWriteException w && w.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return true;
            }
            return false;
        }

        public static IMongoClient Create(this IMongoClientFactory mongoClientFactory, MongoDBOptions options, string defaultName)
        {
            var name = options.ClientName;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = defaultName;
            }

            return mongoClientFactory.Create(name);
        }
    }
}
