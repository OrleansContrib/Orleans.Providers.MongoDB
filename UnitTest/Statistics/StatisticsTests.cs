using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using Orleans.Providers.MongoDB.Statistics;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Statistics
{
    public class MongoStatisticsPublisherTests
    {
        private readonly MongoStatisticsPublisher statisticsPublisher;

        public MongoStatisticsPublisherTests()
        {
            statisticsPublisher = new MongoStatisticsPublisher();
        }

        [Fact]
        public async Task MongoStatisticsPublisher_ReportMetrics_Client()
        {
            await InitProvider(TimeSpan.Zero);
            await TestClientMetrics();
        }

        [Fact]
        public async Task MongoStatisticsPublisher_ReportMetrics_Client_Over_Time()
        {
            await InitProvider(TimeSpan.FromDays(1));
            await TestClientMetrics();
        }

        [Fact]
        public async Task MongoStatisticsPublisher_ReportMetrics_Silo()
        {
            await InitProvider(TimeSpan.Zero);
            await TestSiloMetrics();
        }

        [Fact]
        public async Task MongoStatisticsPublisher_ReportMetrics_Silo_Over_Time()
        {
            await InitProvider(TimeSpan.FromDays(1));
            await TestSiloMetrics();
        }

        [Fact]
        public async Task MongoStatisticsPublisher_ReportStats()
        {
            await InitProvider(TimeSpan.Zero);

            var publisher = (IConfigurableStatisticsPublisher)statisticsPublisher;

            await publisher.Init(true, "mongodb://localhost/OrleansTest", "statisticsDeployment", "statisticsAddress", "statisticsSilo", "statisticsHostName");

            publisher.AddConfiguration("statisticsDeployment", false, "statisticsSilo", SiloAddress.Zero, new IPEndPoint(IPAddress.Loopback, 66), "statisticsHostName");

            await RunParallel(100, () => statisticsPublisher.ReportStats(new List<ICounter> { new DummyCounter(), new DummyCounter() }));
        }

        private async Task TestSiloMetrics()
        {
            var publisher = (IConfigurableSiloMetricsDataPublisher) statisticsPublisher;

            await publisher.Init("statisticsDeployment", "mongodb://localhost/OrleansTest", SiloAddress.Zero, "statisticsSilo", new IPEndPoint(IPAddress.Loopback, 66), "statisticsHostName");

            publisher.AddConfiguration("statisticsDeployment", true, "statisticsSiloId", SiloAddress.Zero, new IPEndPoint(IPAddress.Loopback, 12345), "statisticsHostName");

            await RunParallel(100, () => statisticsPublisher.ReportMetrics((ISiloPerformanceMetrics) new DummyPerformanceMetrics()));
        }

        private async Task TestClientMetrics()
        {
            var publisher = (IConfigurableClientMetricsDataPublisher)statisticsPublisher;

            await publisher.Init(new ClientConfiguration { DataConnectionString = "mongodb://localhost/OrleansTest" }, IPAddress.Loopback, "statisticsClient");

            publisher.AddConfiguration("statisticsDeployment", "statisticsHostName", "statisticsClient", IPAddress.Loopback);

            await RunParallel(100, () => publisher.ReportMetrics(new DummyPerformanceMetrics()));
        }

        private async Task InitProvider(TimeSpan expireAfter)
        {
            var configService = A.Fake<IProviderConfiguration>();
            var configProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["expireAfter"] = expireAfter.ToString()
            };

            A.CallTo(() => configService.Properties).Returns(new ReadOnlyDictionary<string, string>(configProperties));

            await statisticsPublisher.Init("Test", A.Fake<IProviderRuntime>(), configService);
        }

        private static Task RunParallel(int count, Func<Task> taskFactory)
        {
            return Task.WhenAll(Enumerable.Range(0, count).Select(x => taskFactory()));
        }
    }
}