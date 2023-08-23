using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Runtime;
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
                        .AddSingletonNamedService<IGrainStateSerializer, BsonGrainStateSerializer>(ProviderConstants.DEFAULT_PUBSUB_PROVIDER_NAME);

                    siloBuilder
                        .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("OrleansTestStream")
                        .UseLocalhostClustering()
                        .UseMongoDBClient(MongoDatabaseFixture.DatabaseConnectionString)
                        .AddMongoDBGrainStorageAsDefault(options => options.DatabaseName = "OrleansTestApp")
                        .AddMongoDBGrainStorage(ProviderConstants.DEFAULT_PUBSUB_PROVIDER_NAME, options => options.DatabaseName = "OrleansTestApp");
                })
                .Build();

            await host.StartAsync();

            var client = host.Services.GetRequiredService<IClusterClient>();
            var mongoClient = client.ServiceProvider.GetRequiredService<IMongoClient>();
            var database = mongoClient.GetDatabase("OrleansTestApp");
            var collection = database.GetCollection<ConstrainedGrainState>("GrainsConstrainedGrain");

            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<ConstrainedGrainState>(
                    Builders<ConstrainedGrainState>.IndexKeys.Ascending(f => f.Name),
                    new CreateIndexOptions { Unique = true }));

            var guid = Guid.NewGuid().ToString();

            var grain0 = client.GetGrain<IConstrainedGrain>(0);
            await grain0.SetName(guid);

            await database.GetCollection<BsonDocument>("GrainsConstrainedGrain").UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", "constrained/0"),
                    Builders<BsonDocument>.Update.Set("_etag", Guid.NewGuid().ToString()));

            var exception0 = await Assert.ThrowsAsync<ProviderStateException>(async () => await grain0.SetName(Guid.NewGuid().ToString()));
            Assert.Equal("A write operation resulted in an error. WriteError: { Category : \"DuplicateKey\", Code : 11000, Message : \"E11000 duplicate key error collection: OrleansTestApp.GrainsConstrainedGrain index: _id_ dup key: { _id: \"constrained/0\" }\" }.", exception0.Message);

            var grain1 = client.GetGrain<IConstrainedGrain>(1);
            var exception1 = await Assert.ThrowsAsync<ProviderStateException>(async () => await grain1.SetName(guid));
            Assert.Contains("A write operation resulted in an error. WriteError: { Category : \"DuplicateKey\", Code : 11000, Message : \"E11000 duplicate key error collection: OrleansTestApp.GrainsConstrainedGrain index: Name_1 dup key: { Name: null }\" }.", exception1.Message);
        }
    }
}
