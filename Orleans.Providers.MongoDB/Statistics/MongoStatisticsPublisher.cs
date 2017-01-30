namespace Orleans.Providers.MongoDB.Statistics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using global::MongoDB.Driver;

    using Orleans.Providers.MongoDB.Statistics.Repository;
    using Orleans.Runtime;
    using Orleans.Runtime.Configuration;

    /// <summary>
    /// Plugin for publishing silos and client statistics to a Mongo database.
    /// </summary>
    public class MongoStatisticsPublisher : IConfigurableStatisticsPublisher,
                                            IConfigurableSiloMetricsDataPublisher,
                                            IConfigurableClientMetricsDataPublisher,
                                            IProvider
    {
        private string deploymentId;

        private IPAddress clientAddress;

        private SiloAddress siloAddress;

        private IPEndPoint gateway;

        private string clientId;

        private string siloName;

        private string hostName;

        private bool isSilo;

        private long generation;

        private IMongoStatisticsPublisherRepository repository;

        private Logger logger;

        /// <summary>
        /// Name of the provider
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes publisher
        /// </summary>
        /// <param name="name">Provider name</param>
        /// <param name="providerRuntime">Provider runtime API</param>
        /// <param name="config">Provider configuration</param>
        /// <returns></returns>
        public async Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;
            this.logger = providerRuntime.GetLogger("MongoStatisticsPublisher");

            this.repository = new MongoStatisticsPublisherRepository(config.Properties["ConnectionString"], MongoUrl.Create(config.Properties["ConnectionString"]).DatabaseName);
        }

        /// <summary>
        /// Closes provider
        /// </summary>
        /// <returns>Resolved task</returns>
        public Task Close()
        {
            return TaskDone.Done;
        }

        /// <summary>
        /// Adds configuration parameters
        /// </summary>
        /// <param name="deployment">Deployment ID</param>
        /// <param name="hostName">Host name</param>
        /// <param name="client">Client ID</param>
        /// <param name="address">IP address</param>
        public void AddConfiguration(string deployment, string hostName, string client, IPAddress address)
        {
            this.deploymentId = deployment;
            this.isSilo = false;
            this.hostName = hostName;
            this.clientId = client;
            this.clientAddress = address;
            this.generation = SiloAddress.AllocateNewGeneration();
        }

        /// <summary>
        /// Adds configuration parameters
        /// </summary>
        /// <param name="deployment">Deployment ID</param>
        /// <param name="silo">Silo name</param>
        /// <param name="siloId">Silo ID</param>
        /// <param name="address">Silo address</param>
        /// <param name="gatewayAddress">Client gateway address</param>
        /// <param name="hostName">Host name</param>
        public void AddConfiguration(
            string deployment,
            bool silo,
            string siloId,
            SiloAddress address,
            IPEndPoint gatewayAddress,
            string hostName)
        {
            this.deploymentId = deployment;
            this.isSilo = silo;
            this.siloName = siloId;
            this.siloAddress = address;
            this.gateway = gatewayAddress;
            this.hostName = hostName;
            if (!this.isSilo)
            {
                this.generation = SiloAddress.AllocateNewGeneration();
            }
        }


        async Task IClientMetricsDataPublisher.Init(ClientConfiguration config, IPAddress address, string clientId)
        {
            this.repository = new MongoStatisticsPublisherRepository(config.DataConnectionString, MongoUrl.Create(config.DataConnectionString).DatabaseName);
        }

        /// <summary>
        /// Writes metrics to the database
        /// </summary>
        /// <param name="metricsData">Metrics data</param>
        /// <returns>Task for database operation</returns>
        public async Task ReportMetrics(IClientPerformanceMetrics metricsData)
        {
            if (this.logger != null && this.logger.IsVerbose3)
                this.logger.Verbose3(
                    "MongoStatisticsPublisher.ReportMetrics (client) called with data: {0}.",
                    metricsData);
            try
            {
                int gateWayPort = 0;

                if (this.gateway != null)
                {
                    gateWayPort = this.gateway.Port;
                }

                await this.repository.UpsertReportClientMetricsAsync(
                        new OrleansClientMetricsTable
                            {
                                DeploymentId = this.deploymentId,
                                ClientId = this.clientId,
                                Address = this.clientAddress.MapToIPv4().ToString(),
                                HostName = this.hostName,
                                GatewayPort = gateWayPort,
                                //Port = this.siloAddress.,
                                Generation = this.generation
                        }, 
                            metricsData);
            }
            catch (Exception ex)
            {
                if (this.logger != null && this.logger.IsVerbose)
                {
                    this.logger.Verbose("MongoStatisticsPublisher.ReportMetrics (client) failed: {0}", ex);
                }

                throw;
            }
        }


        Task ISiloMetricsDataPublisher.Init(
            string deploymentId,
            string storageConnectionString,
            SiloAddress siloAddress,
            string siloName,
            IPEndPoint gateway,
            string hostName)
        {
            this.gateway = gateway;
            return TaskDone.Done;
        }

        /// <summary>
        /// Writes silo performance metrics to the database
        /// </summary>
        /// <param name="metricsData">Metrics data</param>
        /// <returns>Task for database operation</returns>
        public async Task ReportMetrics(ISiloPerformanceMetrics metricsData)
        {
            if (this.logger != null && this.logger.IsVerbose3) this.logger.Verbose3("MongoStatisticsPublisher.ReportMetrics (silo) called with data: {0}.", metricsData);
            try
            {
                int gateWayPort = 0;

                if (this.gateway != null)
                {
                    gateWayPort = this.gateway.Port;
                }

                await this.repository.UpsertSiloMetricsAsync(
                    new OrleansSiloMetricsTable
                        {
                            DeploymentId = this.deploymentId,
                            SiloId = this.siloName,
                            GatewayAddress = this.gateway.Address.MapToIPv4().ToString(),
                            HostName = this.hostName,
                            GatewayPort = gateWayPort,
                            Port = this.siloAddress.Endpoint.Port,
                            Generation = this.generation
                    }, 
                    metricsData);
            }
            catch (Exception ex)
            {
                if (this.logger != null && this.logger.IsVerbose) this.logger.Verbose("MongoStatisticsPublisher.ReportMetrics (silo) failed: {0}", ex);
                throw;
            }
        }


        Task IStatisticsPublisher.Init(
            bool isSilo,
            string storageConnectionString,
            string deploymentId,
            string address,
            string siloName,
            string hostName)
        {
            return TaskDone.Done;
        }

        /// <summary>
        /// Writes statistics to the database
        /// </summary>
        /// <param name="statsCounters">Statistics counters to write</param>
        /// <returns>Task for database opearation</returns>
        public async Task ReportStats(List<ICounter> statsCounters)
        {
            var siloOrClientName = (this.isSilo) ? this.siloName : this.clientId;
            var id = (this.isSilo)
                         ? this.siloAddress.ToLongString()
                         : string.Format("{0}:{1}", siloOrClientName, this.generation);
            if (this.logger != null && this.logger.IsVerbose3)
                this.logger.Verbose3(
                    "ReportStats called with {0} counters, name: {1}, id: {2}",
                    statsCounters.Count,
                    siloOrClientName,
                    id);
            var insertTasks = new List<Task>();
            try
            {
                //This batching is done for two reasons:
                //1) For not to introduce a query large enough to be rejected.
                //2) Performance, though using a fixed constants likely will not give the optimal performance in every situation.
                const int maxBatchSizeInclusive = 200;
                var counterBatches = BatchCounters(statsCounters, maxBatchSizeInclusive);
                foreach (var counterBatch in counterBatches)
                {
                    //The query template from which to retrieve the set of columns that are being inserted.

                    await this.repository.InsertStatisticsCountersAsync(
                        new OrleansStatisticsTable
                            {
                                DeploymentId = this.deploymentId,
                                HostName = this.hostName,
                                Name = siloOrClientName,
                                Id = id
                        }, 
                        counterBatch);
                }

                await Task.WhenAll(insertTasks);
            }
            catch (Exception ex)
            {
                if (this.logger != null && this.logger.IsVerbose) this.logger.Verbose("ReportStats faulted: {0}", ex.ToString());
                foreach (var faultedTask in insertTasks.Where(t => t.IsFaulted))
                {
                    if (this.logger != null && this.logger.IsVerbose) this.logger.Verbose("Faulted task exception: {0}", faultedTask.ToString());
                }

                throw;
            }

            if (this.logger != null && this.logger.IsVerbose) this.logger.Verbose("ReportStats SUCCESS");
        }


        /// <summary>
        /// Batches the counters list to batches of given maximum size.
        /// </summary>
        /// <param name="counters">The counters to batch.</param>
        /// <param name="maxBatchSizeInclusive">The maximum size of one batch.</param>
        /// <returns>The counters batched.</returns>
        private static List<List<ICounter>> BatchCounters(List<ICounter> counters, int maxBatchSizeInclusive)
        {
            var batches = new List<List<ICounter>>();
            for (int i = 0; i < counters.Count; i += maxBatchSizeInclusive)
            {
                batches.Add(counters.GetRange(i, Math.Min(maxBatchSizeInclusive, counters.Count - i)));
            }

            return batches;
        }
    }
}