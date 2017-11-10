using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Providers.MongoDB.Statistics.Store;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

// ReSharper disable InheritdocInvalidUsage
// ReSharper disable ArrangeThisQualifier
// ReSharper disable ConvertToLambdaExpression

namespace Orleans.Providers.MongoDB.Statistics
{
    public class MongoStatisticsPublisher :
        IConfigurableStatisticsPublisher,
        IConfigurableSiloMetricsDataPublisher,
        IConfigurableClientMetricsDataPublisher,
        IProvider
    {
        public const string ConnectionStringProperty = "ConnectionString";
        public const string CollectionPrefixProperty = "CollectionPrefix";
        public const string DatabaseNameProperty = "DatabaseProperty";
        public const string ExpireAfterProperty = "ExpireAfter";

        private TimeSpan mongoExpireAfter;
        private string mongoConnectionString;
        private string mongoDatabaseName;
        private string mongoCollectionPrefix;
        private string configuredDeploymentId;
        private string configuredClientAddress;
        private string configuredSiloAddress;
        private string configuredClientId;
        private string configuredSiloName;
        private string configuredHostName;
        private string configuredGatewayAddress;
        private int configuredGeneration;
        private bool configuredIsSilo;
        private ILogger logger;
        private MongoClientMetricsCollection clientMetricsCollection;
        private MongoSiloMetricsCollection siloMetricsCollection;
        private MongoStatisticsCounterCollection statisticsCounterCollection;

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public Task Close()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;

            logger = providerRuntime.ServiceProvider.GetRequiredService<ILogger<MongoStatisticsPublisher>>();

            var expireAfter = config.GetProperty(ExpireAfterProperty, string.Empty);

            TimeSpan.TryParse(expireAfter, out mongoExpireAfter);

            mongoConnectionString = config.GetProperty(ConnectionStringProperty, string.Empty);
            mongoCollectionPrefix = config.GetProperty(CollectionPrefixProperty, string.Empty);
            mongoDatabaseName = config.GetProperty(DatabaseNameProperty, string.Empty);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void AddConfiguration(string deploymentId, string hostName, string clientId, IPAddress address)
        {
            this.configuredIsSilo = false;

            this.configuredDeploymentId = deploymentId;
            this.configuredHostName = hostName;
            this.configuredClientId = clientId;
            this.configuredClientAddress = address.MapToIPv4().ToString();
            this.configuredGeneration = SiloAddress.AllocateNewGeneration();
        }

        /// <inheritdoc />
        public void AddConfiguration(string deploymentId, bool isSilo, string siloName, SiloAddress address, IPEndPoint gateway, string hostName)
        {
            this.configuredIsSilo = isSilo;

            this.configuredDeploymentId = deploymentId;
            this.configuredSiloName = siloName;
            this.configuredSiloAddress = address.ToLongString();
            this.configuredGatewayAddress = $"{gateway.Address.MapToIPv4()}:{gateway.Port}";
            this.configuredHostName = hostName;

            if (!isSilo)
            {
                configuredGeneration = SiloAddress.AllocateNewGeneration();
            }
        }

        /// <inheritdoc />
        Task IClientMetricsDataPublisher.Init(ClientConfiguration config, IPAddress address, string clientId)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        Task ISiloMetricsDataPublisher.Init(string deploymentId, string storageConnectionString, SiloAddress siloAddress, string siloName, IPEndPoint gateway, string hostName)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        Task IStatisticsPublisher.Init(bool isSilo, string storageConnectionString, string deploymentId, string address, string siloName, string hostName)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ReportMetrics(IClientPerformanceMetrics metricsData)
        {
            if (clientMetricsCollection == null)
            {
                clientMetricsCollection =
                    new MongoClientMetricsCollection(
                        mongoConnectionString,
                        mongoDatabaseName,
                        mongoExpireAfter,
                        mongoCollectionPrefix);
            }

            await DoAndLog(nameof(ReportMetrics), () =>
            {
                return clientMetricsCollection.UpsertReportClientMetricsAsync(
                    configuredDeploymentId,
                    configuredClientId,
                    configuredClientAddress,
                    configuredHostName,
                    metricsData);
            });
        }

        /// <inheritdoc />
        public async Task ReportMetrics(ISiloPerformanceMetrics metricsData)
        {
            if (siloMetricsCollection == null)
            {
                siloMetricsCollection =
                    new MongoSiloMetricsCollection(
                        mongoConnectionString,
                        mongoDatabaseName,
                        mongoExpireAfter,
                        mongoCollectionPrefix);
            }

            await DoAndLog(nameof(Init), () =>
            {
                return siloMetricsCollection.UpsertSiloMetricsAsync(
                    configuredDeploymentId,
                    configuredSiloName,
                    configuredSiloAddress,
                    configuredGatewayAddress,
                    configuredHostName,
                    configuredGeneration,
                    metricsData);
            });
        }

        /// <inheritdoc />
        public Task ReportStats(List<ICounter> statsCounters)
        {
             if (statisticsCounterCollection == null)
            {
                statisticsCounterCollection =
                    new MongoStatisticsCounterCollection(
                        mongoConnectionString,
                        mongoDatabaseName,
                        mongoCollectionPrefix);
            }

            return DoAndLog(nameof(ReportStats), () =>
            {
                var siloOrClientName =
                    configuredIsSilo ?
                        configuredSiloName :
                        configuredClientId;

                var siloOrClientId =
                    configuredIsSilo ?
                        configuredSiloAddress :
                        string.Format("{0}:{1}", siloOrClientName, configuredGeneration);

                const int maxBatchSizeInclusive = 200;

                var batchedTasks = new List<Task>();
                var batches = BatchCounters(statsCounters, maxBatchSizeInclusive);

                foreach (var counterBatch in batches)
                {
                    batchedTasks.Add(
                        statisticsCounterCollection.InsertStatisticsCountersAsync(
                            configuredDeploymentId,
                            configuredHostName,
                            siloOrClientName,
                            siloOrClientId,
                            counterBatch));
                }

                return Task.WhenAll(batchedTasks);
            });
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.Warn((int)MongoProviderErrorCode.StatisticsPublisher_Operations, $"{nameof(MongoStatisticsPublisher)}.{actionName} failed. Exception={ex.Message}", ex);
                
                throw;
            }
        }
        private static IEnumerable<List<ICounter>> BatchCounters(List<ICounter> counters, int maxBatchSizeInclusive)
        {
            var batches = new List<List<ICounter>>();

            for (var i = 0; i < counters.Count; i += maxBatchSizeInclusive)
            {
                batches.Add(counters.GetRange(i, Math.Min(maxBatchSizeInclusive, counters.Count - i)));
            }

            return batches;
        }
    }
}