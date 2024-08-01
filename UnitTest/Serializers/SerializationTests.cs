using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Runtime;
using Orleans.Streams;
using TestExtensions;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Serializers
{
    [Collection(TestEnvironmentFixture.DefaultCollection)]
    public partial class SerializationTests
    {
        [Fact]
        public void CorrectSerializerIsUsed()
        {
            var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) =>
                {
                    siloBuilder.Services
                        .AddKeyedSingleton<IGrainStateSerializer, BsonGrainStateSerializer>("BsonProvider")
                        .AddKeyedSingleton<IGrainStateSerializer, BinaryGrainStateSerializer>("BinaryProvider");

                    siloBuilder
                        .UseLocalhostClustering()
                        .AddMongoDBGrainStorage("BsonProvider")
                        .AddMongoDBGrainStorage("BinaryProvider");
                })
                .Build();

            var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>();
            Assert.IsType<JsonGrainStateSerializer>(optionsMonitor.CurrentValue.GrainStateSerializer);
            Assert.IsType<BsonGrainStateSerializer>(optionsMonitor.Get("BsonProvider").GrainStateSerializer);
            Assert.IsType<BinaryGrainStateSerializer>(optionsMonitor.Get("BinaryProvider").GrainStateSerializer);
        }

        [Fact]
        public void CanCustomizeJsonSerializerSettings()
        {
            var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) =>
                {
                    siloBuilder
                        .UseLocalhostClustering()
                        .AddMongoDBGrainStorage(
                            "JsonProvider",
                            options => options.Configure<IServiceProvider>((options, sp) =>
                            {
                                options.GrainStateSerializer = new JsonGrainStateSerializer(
                                    Options.Create(new JsonGrainStateSerializerOptions
                                    {
                                        ConfigureJsonSerializerSettings = settings =>
                                        {
                                            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
                                        }
                                    }),
                                    sp);
                            }));
                })
                .Build();

            var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>();
            Assert.IsType<JsonGrainStateSerializer>(optionsMonitor.Get("JsonProvider").GrainStateSerializer);
        }

        [Fact]
        public async Task BsonSerializerForPubSubStore_Throws()
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
                        .AddMongoDBGrainStorage(ProviderConstants.DEFAULT_PUBSUB_PROVIDER_NAME, options =>
                        {
                            options.DatabaseName = "OrleansTestAppPubSubStore";
                        });
                })
                .Build();

            await host.StartAsync();

            var client = host.Services.GetRequiredService<IClusterClient>();

            var streamProvider = client.GetStreamProvider("OrleansTestStream");
            var stream = streamProvider.GetStream<TestData>(StreamId.Create("ns", "key"));
            var orleansException = await Assert.ThrowsAsync<OrleansException>(async () => await stream.SubscribeAsync((value, token) => Task.CompletedTask));
            var exception = Assert.IsType<NotImplementedException>(orleansException.InnerException);
            Assert.Equal("BsonGrainStateSerializer does not support PubSubStore storage provider, use BinaryGrainStateSerializer or JsonGrainStateSerializer instead.", exception.Message);
        }

        [GenerateSerializer]
        public class TestData
        {
            [Id(0)]
            public string Value { get; set; }
        }
    }
}
