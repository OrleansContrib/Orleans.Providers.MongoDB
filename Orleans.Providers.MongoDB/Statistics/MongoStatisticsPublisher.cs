using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Statistics.Store;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

// ReSharper disable ConvertToLambdaExpression

// HINT: There is no Client st

namespace Orleans.Providers.MongoDB.Statistics
{
    public class MongoStatisticsPublisher :
        IConfigurableStatisticsPublisher,
        IConfigurableSiloMetricsDataPublisher,
        IConfigurableClientMetricsDataPublisher,
        IProvider
    {
        private abstract class Config
        {
            public string HostName;
            public string DeploymentId;
        }

        private sealed class ClientStatsConfig : Config
        {
            public string ClientId;
            public string ClientAddress;
        }

        private sealed class ReportConfig : Config
        {
            public string SiloOrClientId;
            public string SiloOrClientName;
        }

        private sealed class SiloStatsConfig : Config
        {
            public string GatewayAddress;
            public string SiloName;
            public string SiloAddress;
            public int GatewayPort;
            public int SiloPort;
            public int Generation;
        }

        private ClientStatsConfig clientStatsConfig;
        private SiloStatsConfig siloStatsConfig;
        private ReportConfig reportConfig;
        private Logger logger;
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

            logger = providerRuntime.GetLogger("MongoStatisticsPublisher");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        void IConfigurableClientMetricsDataPublisher.AddConfiguration(string deploymentId, string hostName, string clientId, IPAddress address)
        {
            clientStatsConfig = new ClientStatsConfig
            {
                DeploymentId = deploymentId,
                ClientId = clientId,
                ClientAddress = address?.MapToIPv4().ToString(),
                HostName = hostName
            };
        }

        /// <inheritdoc />
        void IConfigurableSiloMetricsDataPublisher.AddConfiguration(string deploymentId, bool isSilo, string siloName, SiloAddress address, IPEndPoint gateway, string hostName)
        {
            siloStatsConfig = new SiloStatsConfig
            {
                DeploymentId = deploymentId,
                GatewayAddress = gateway?.Address.MapToIPv4().ToString(),
                GatewayPort = gateway?.Port ?? 0,
                Generation = address?.Generation ?? 0,
                HostName = hostName,
                SiloAddress = address?.Endpoint.Address.MapToIPv4().ToString(),
                SiloName = siloName,
                SiloPort = address?.Endpoint.Port ?? 0,
            };
        }

        /// <inheritdoc />
        void IConfigurableStatisticsPublisher.AddConfiguration(string deploymentId, bool isSilo, string siloName, SiloAddress address, IPEndPoint gateway, string hostName)
        {
            reportConfig = new ReportConfig
            {
                DeploymentId = deploymentId,
                SiloOrClientName = siloName,
                SiloOrClientId = address.ToLongString(),
                HostName = hostName
            };
        }

        /// <inheritdoc />
        Task IClientMetricsDataPublisher.Init(ClientConfiguration config, IPAddress address, string clientId)
        {
            return DoAndLog(nameof(Init), () =>
            {
                clientMetricsCollection =
                    new MongoClientMetricsCollection(config.DataConnectionString,
                        MongoUrl.Create(config.DataConnectionString).DatabaseName, null);

                return Task.CompletedTask;
            });
        }

        /// <inheritdoc />
        Task ISiloMetricsDataPublisher.Init(string deploymentId, string storageConnectionString, SiloAddress siloAddress, string siloName, IPEndPoint gateway, string hostName)
        {
            return DoAndLog(nameof(Init), () =>
            {
                siloMetricsCollection =
                    new MongoSiloMetricsCollection(storageConnectionString,
                        MongoUrl.Create(storageConnectionString).DatabaseName, null);

                return Task.CompletedTask;
            });
        }

        /// <inheritdoc />
        Task IStatisticsPublisher.Init(bool isSilo, string storageConnectionString, string deploymentId, string address, string siloName, string hostName)
        {
            return DoAndLog(nameof(Init), () =>
            {
                statisticsCounterCollection =
                    new MongoStatisticsCounterCollection(storageConnectionString,
                        MongoUrl.Create(storageConnectionString).DatabaseName);

                return Task.CompletedTask;
            });
        }

        /// <inheritdoc />
        public Task ReportMetrics(IClientPerformanceMetrics metricsData)
        {
            return DoAndLog(nameof(ReportMetrics), () =>
            {
                return clientMetricsCollection.UpsertReportClientMetricsAsync(
                    clientStatsConfig.DeploymentId,
                    clientStatsConfig.ClientId,
                    clientStatsConfig.ClientAddress,
                    clientStatsConfig.HostName,
                    metricsData);
            });
        }

        /// <inheritdoc />
        public Task ReportMetrics(ISiloPerformanceMetrics metricsData)
        {
            return DoAndLog(nameof(Init), () =>
            {
                return siloMetricsCollection.UpsertSiloMetricsAsync(
                    siloStatsConfig.DeploymentId,
                    siloStatsConfig.SiloName,
                    siloStatsConfig.SiloAddress,
                    siloStatsConfig.SiloPort,
                    siloStatsConfig.GatewayAddress,
                    siloStatsConfig.GatewayPort,
                    siloStatsConfig.HostName,
                    siloStatsConfig.Generation,
                    metricsData);
            });
        }

        /// <inheritdoc />
        public Task ReportStats(List<ICounter> statsCounters)
        {
            return DoAndLog(nameof(ReportStats), () =>
            {
                return statisticsCounterCollection.InsertStatisticsCountersAsync(
                    reportConfig.DeploymentId,
                    reportConfig.HostName,
                    reportConfig.SiloOrClientName,
                    reportConfig.SiloOrClientId,
                    statsCounters);
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
                if (logger.IsVerbose)
                {
                    logger.Warn((int)MongoProviderErrorCode.StatisticsPublisher_Operations, $"MongoStatisticsPublisher.{actionName} failed. Exception={ex.Message}", ex);
                }

                throw;
            }
        }
    }
}