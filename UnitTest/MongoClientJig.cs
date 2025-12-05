using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Providers.MongoDB.Utils;

namespace Orleans.Providers.MongoDB.UnitTest;

public class MongoClientJig
{
    public IMongoClientFactory CreateDatabaseFactory()
    {
        return new DefaultMongoClientFactory(new MongoClient(MongoDatabaseFixture.DatabaseConnectionString));
    }

    public IMongoClientFactory CreateReplicaSetFactory()
    {
        return new DefaultMongoClientFactory(new MongoClient(MongoDatabaseFixture.ReplicaSetConnectionString));
    }

    public Task AssertQualityChecksAsync()
    {
        return Task.CompletedTask;
    }
}