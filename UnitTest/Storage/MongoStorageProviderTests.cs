using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.TestingHost;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Storage
{
    public partial class MongoStorageProviderTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_store_and_read_state(bool useJson)
        {
            var cluster = CreateTestCluster(useJson);

            var counter = cluster.GrainFactory.GetGrain<IStatefulGrain>(Guid.NewGuid());

            await counter.Increment();
            await counter.Increment();
            await counter.Increment();
            await counter.Decrement();

            Assert.Equal(2, await counter.Count());

            await counter.Teardown();
            await Task.Delay(100);

            Assert.Equal(2, await counter.Count());

            cluster.StopAllSilos();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_clear_data_and_return_default_state(bool useJson)
        {
            var cluster = CreateTestCluster(useJson);

            var counter = cluster.GrainFactory.GetGrain<IStatefulGrain>(Guid.NewGuid());

            await counter.Increment();
            await counter.Increment();
            await counter.Clear();

            Assert.Equal(0, await counter.Count());

            await counter.Teardown();
            await Task.Delay(100);

            Assert.Equal(0, await counter.Count());

            cluster.StopAllSilos();
        }

        private static TestCluster CreateTestCluster(bool useJson)
        {
            var config = new Dictionary<string, string>
            {
                ["ConnectionString"] = "mongodb://localhost/OrleansTest",
                ["UseJsonFormat"] = useJson.ToString()
            };

            var cluster = new TestCluster();

            cluster.ClusterConfiguration.Globals.RegisterStorageProvider<MongoStorageProvider>("Default", config);
            cluster.Deploy();

            return cluster;
        }
    }
}