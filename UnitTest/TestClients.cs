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
                new MongoClient("mongodb+srv://admin:1q2w3e4r@cluster0.frfuhnp.mongodb.net/admin?replicaSet=atlas-6fz56o-shard-0&readPreference=primary&connectTimeoutMS=10000&authSource=admin&authMechanism=SCRAM-SHA-1&3t.uriVersion=3&3t.connection.name=atlas&3t.databases=admin&3t.alwaysShowAuthDB=true&3t.alwaysShowDBFromUserRole=true&3t.sslTlsVersion=TLS")));
    }
}
