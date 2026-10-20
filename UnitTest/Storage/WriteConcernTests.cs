using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Storage
{
    [Collection(TestEnvironmentFixture.DefaultCollection)]
    public class WriteConcernTests
    {
        private const string DatabaseName = "OrleansWriteConcernTests";

        [Fact]
        public async Task KeepsTheWrittenETagWhenOnlyTheWriteConcernFails()
        {
            var grainId = GrainId.Create("counter", Guid.NewGuid().ToString());
            var grainState = new GrainState<CounterState> { State = new CounterState { Value = 1 } };

            // A single-node replica set can never acknowledge w:2, so the write is applied and then fails on its write concern.
            var unsatisfiable = CreateStorage(settings => settings.WriteConcern = new WriteConcern(2, wTimeout: TimeSpan.FromMilliseconds(200)));

            var writeConcernFailure = await Assert.ThrowsAsync<MongoWriteException>(() => unsatisfiable.WriteStateAsync("Counter", grainId, grainState));
            Assert.Null(writeConcernFailure.WriteError);
            Assert.NotNull(writeConcernFailure.WriteConcernError);

            var stored = await new MongoClient(MongoDatabaseFixture.ReplicaSetConnectionString)
                .GetDatabase(DatabaseName)
                .GetCollection<BsonDocument>("GrainsCounter")
                .Find(Builders<BsonDocument>.Filter.Eq("_id", grainId.ToString()))
                .FirstAsync();
            Assert.Equal(stored["_etag"].AsString, grainState.ETag);

            // With the old ETag the next write misses the _etag filter, upserts the same _id and fails with E11000.
            var satisfiable = CreateStorage(collectionConfigurator: null);
            grainState.State.Value = 2;
            var nextWriteFailure = await Record.ExceptionAsync(() => satisfiable.WriteStateAsync("Counter", grainId, grainState));
            Assert.Null(nextWriteFailure);
        }

        private static MongoGrainStorage CreateStorage(Action<MongoCollectionSettings> collectionConfigurator)
        {
            return new MongoGrainStorage(
                MongoDatabaseFixture.ReplicaSetFactory,
                NullLogger<MongoGrainStorage>.Instance,
                new MongoDBGrainStorageOptions
                {
                    DatabaseName = DatabaseName,
                    CollectionConfigurator = collectionConfigurator,
                    GrainStateSerializer = new BsonGrainStateSerializer()
                });
        }

        public class CounterState
        {
            public int Value { get; set; }
        }
    }
}
