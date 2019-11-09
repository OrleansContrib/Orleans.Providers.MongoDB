using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using System;

namespace Orleans.Providers.MongoDB.UnitTest
{
    public static class TestClients
    {
        public static readonly Lazy<IMongoClientFactory> Localhost = new Lazy<IMongoClientFactory>(() =>
            new DefaultMongoClientFactory(
                new MongoClient("mongodb://localhost/OrleansTest")));

        // ReplicaSet on MongoAtlas with IP whitelist.
        public static readonly Lazy<IMongoClientFactory> ReplicaSet = new Lazy<IMongoClientFactory>(() =>
            new DefaultMongoClientFactory(
                new MongoClient("mongodb+srv://foo:1q2w3e$R@squidex-jlpkt.mongodb.net/test?retryWrites=true&w=majority")));
    }
}
