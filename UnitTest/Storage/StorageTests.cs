using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using System;
using System.Threading.Tasks;
using TestExtensions;
using Xunit;
using static Orleans.Providers.MongoDB.UnitTest.Storage.TestGrains.StorageTests;

namespace Orleans.Providers.MongoDB.UnitTest.Storage
{
    [Collection(TestEnvironmentFixture.DefaultCollection)]
    public partial class StorageTests
    {
        [Fact]
        public async Task ThrowErrorWhenUpdatingCollectionWithAUniqueIndex()
        {
            var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) =>
                {
                    siloBuilder.Services
                        .AddKeyedSingleton<IGrainStateSerializer, BsonGrainStateSerializer>(ProviderConstants.DEFAULT_PUBSUB_PROVIDER_NAME);

                    siloBuilder
                        .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("OrleansTestStream")
                        .UseLocalhostClustering()
                        .UseMongoDBClient(MongoDatabaseFixture.DatabaseConnectionString)
                        .AddMongoDBGrainStorageAsDefault(options => options.DatabaseName = "OrleansTestApp")
                        .AddMongoDBGrainStorage(ProviderConstants.DEFAULT_PUBSUB_PROVIDER_NAME, options => options.DatabaseName = "OrleansTestApp");
                })
                .Build();

            await host.StartAsync(TestContext.Current.CancellationToken);

            var client = host.Services.GetRequiredService<IClusterClient>();
            var mongoClient = client.ServiceProvider.GetRequiredService<IMongoClient>();
            var database = mongoClient.GetDatabase("OrleansTestApp");
            var collection = database.GetCollection<ConstrainedGrainState>("GrainsConstrainedGrain");

            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<ConstrainedGrainState>(
                    Builders<ConstrainedGrainState>.IndexKeys.Ascending(f => f.Name),
                    new CreateIndexOptions { Unique = true }),
                cancellationToken: TestContext.Current.CancellationToken);

            var guid = Guid.NewGuid().ToString();

            var grain0 = client.GetGrain<IConstrainedGrain>(0);
            await grain0.SetName(guid);

            await database.GetCollection<BsonDocument>("GrainsConstrainedGrain").UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", "constrained/0"),
                    Builders<BsonDocument>.Update.Set("_etag", Guid.NewGuid().ToString()),
                    cancellationToken: TestContext.Current.CancellationToken);

            var exception0 = await Assert.ThrowsAsync<ProviderStateException>(async () => await grain0.SetName(Guid.NewGuid().ToString()));
            AssertDuplicateKeyError(exception0, "index: _id_ dup key: { _id: \"constrained/0\" }");

            var grain1 = client.GetGrain<IConstrainedGrain>(1);
            var exception1 = await Assert.ThrowsAsync<ProviderStateException>(async () => await grain1.SetName(guid));
            AssertDuplicateKeyError(exception1, "index: Name_1 dup key: { Name: null }");
        }

        [Fact]
        public async Task LegacyDocumentWithoutEtagCanBeReadOverwrittenAndCleared()
        {
            using var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) =>
                {
                    siloBuilder
                        .UseLocalhostClustering(siloPort: 11113, gatewayPort: 30002)
                        .UseMongoDBClient(MongoDatabaseFixture.DatabaseConnectionString)
                        .AddMongoDBGrainStorageAsDefault(options => options.DatabaseName = "OrleansTestAppLegacy");
                })
                .Build();

            await host.StartAsync(TestContext.Current.CancellationToken);

            var grain = host.Services.GetRequiredService<IClusterClient>().GetGrain<IConstrainedGrain>(Random.Shared.NextInt64());
            var grainKey = grain.GetGrainId().ToString();
            var collection = host.Services.GetRequiredService<IMongoClient>().GetDatabase("OrleansTestAppLegacy").GetCollection<BsonDocument>("GrainsConstrainedGrain");

            // Documents from before the _etag/_doc fields were introduced store the state at the root.
            await collection.InsertOneAsync(new BsonDocument { ["_id"] = grainKey, ["Name"] = "legacy" }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("legacy", await grain.GetName());

            await grain.SetName("migrated");
            Assert.Equal("migrated", await grain.GetName());
            await grain.SetName("migrated again");

            await grain.ClearName();
            Assert.Null(await grain.GetName());
            Assert.Equal(0, await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("_id", grainKey), cancellationToken: TestContext.Current.CancellationToken));

            await host.StopAsync(TestContext.Current.CancellationToken);
        }

        // Newer servers (e.g. MongoDB 8.3) prefix the server message with "Plan executor error during update :: caused by :: ",
        // so only the stable parts of the write error are verified.
        private static void AssertDuplicateKeyError(Exception exception, string duplicateKey)
        {
            Assert.Contains("A write operation resulted in an error. WriteError: { Category : \"DuplicateKey\", Code : 11000, Message : \"", exception.Message);
            Assert.Contains($"E11000 duplicate key error collection: OrleansTestApp.GrainsConstrainedGrain {duplicateKey}\" }}.", exception.Message);
        }

        [Fact]
        public async Task ClearStateResetsGrainState()
        {
            using var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) =>
                {
                    siloBuilder
                        .UseLocalhostClustering(siloPort: 11112, gatewayPort: 30001)
                        .UseMongoDBClient(MongoDatabaseFixture.DatabaseConnectionString)
                        .AddMongoDBGrainStorageAsDefault(options => options.DatabaseName = "OrleansTestAppClear");
                })
                .Build();

            await host.StartAsync(TestContext.Current.CancellationToken);

            var grain = host.Services.GetRequiredService<IClusterClient>().GetGrain<IConstrainedGrain>(Random.Shared.NextInt64());

            await grain.SetName("value");
            Assert.Equal("value", await grain.GetName());

            await grain.ClearName();
            Assert.Null(await grain.GetName());

            await grain.SetName("other");
            Assert.Equal("other", await grain.GetName());

            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
