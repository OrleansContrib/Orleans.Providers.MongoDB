using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.Utils
{
    public sealed class DefaultMongoClientFactory : IMongoClientFactory
    {
        private readonly IMongoClient mongoClient;

        public DefaultMongoClientFactory(IMongoClient mongoClient)
        {
            this.mongoClient = mongoClient;
        }

        public IMongoClient Create(string name)
        {
            return mongoClient;
        }
    }
}
