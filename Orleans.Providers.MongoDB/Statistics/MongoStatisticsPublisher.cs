using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Statistics.Repository;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace Orleans.Providers.MongoDB.Statistics
{
    /// <summary>
    ///     Plugin for publishing silos and client statistics to a Mongo database.
    /// </summary>
    public class MongoStatisticsPublisher : IConfigurableStatisticsPublisher,
        IConfigurableSiloMetricsDataPublisher,
        IConfigurableClientMetricsDataPublisher,
        IProvider
    {
        private IPAddress clientAddress;

        private string clientId;
        private string deploymentId;

        private IPEndPoint gateway;

        private long generation;

        private string hostName;

        private bool isSilo;

        private Logger logger;

        private IMongoStatisticsPublisherRepository repository;

        private SiloAddress siloAddress;

        private string siloName;

        /// <summary>
        ///     Adds configuration parameters
        /// </summary>
        /// <param name="deployment">Deployment ID</param>
        /// <param name="hostName">Host name</param>
        /// <param name="client">Client ID</param>
        /// <param name="address">IP address</param>
        public void AddConfiguration(string deployment, string hostName, string client, IPAddress address)
        {
            deploymentId = deployment;
            isSilo = false;
            this.hostName = hostName;
            clientId = client;
            clientAddress = address;
            generation = SiloAddress.AllocateNewGeneration();
        }

        /// <summary>
        ///     The Client init.
        /// </summary>
        /// <param name="config">
        ///     The config.
        /// </param>
        /// <param name="address">
        ///     The address.
        /// </param>
        /// <param name="clientId">
        ///     The client id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task IClientMetricsDataPublisher.Init(ClientConfiguration config, IPAddress address, string clientId)
        {
            repository = new MongoStatisticsPublisherRepository(config.DataConnectionString,
                MongoUrl.Create(config.DataConnectionString).DatabaseName);
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Writes metrics to the database
        /// </summary>
        /// <param name="metricsData">Metrics data</param>
        /// <returns>Task for database operation</returns>
        public async Task ReportMetrics(IClientPerformanceMetrics metricsData)
        {
            if (logger != null && logger.IsVerbose3)
                logger.Verbose3(
                    "MongoStatisticsPublisher.ReportMetrics (client) called with data: {0}.",
                    metricsData);
            try
            {
                await repository.UpsertReportClientMetricsAsync(
                    new OrleansClientMetricsTable
                    {
                        DeploymentId = deploymentId,
                        ClientId = clientId,
                        Address = clientAddress.MapToIPv4().ToString(),
                        HostName = hostName
                    },
                    metricsData);
            }
            catch (Exception ex)
            {
                if (logger != null && logger.IsVerbose)
                    logger.Verbose("MongoStatisticsPublisher.ReportMetrics (client) failed: {0}", ex);

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
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Writes silo performance metrics to the database
        /// </summary>
        /// <param name="metricsData">Metrics data</param>
        /// <returns>Task for database operation</returns>
        public async Task ReportMetrics(ISiloPerformanceMetrics metricsData)
        {
            if (logger != null && logger.IsVerbose3)
                logger.Verbose3("MongoStatisticsPublisher.ReportMetrics (silo) called with data: {0}.", metricsData);
            try
            {
                var gateWayPort = 0;

                if (gateway != null)
                    gateWayPort = gateway.Port;

                await repository.UpsertSiloMetricsAsync(
                    new OrleansSiloMetricsTable
                    {
                        DeploymentId = deploymentId,
                        SiloId = siloName,
                        GatewayAddress = gateway.Address.MapToIPv4().ToString(),
                        HostName = hostName,
                        GatewayPort = gateWayPort,
                        Port = siloAddress.Endpoint.Port,
                        Generation = generation,
                        Address = siloAddress.Endpoint.Address.MapToIPv4().ToString()
                    },
                    metricsData);
            }
            catch (Exception ex)
            {
                if (logger != null && logger.IsVerbose)
                    logger.Verbose("MongoStatisticsPublisher.ReportMetrics (silo) failed: {0}", ex);
                throw;
            }
        }

        /// <summary>
        ///     Adds configuration parameters
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
            deploymentId = deployment;
            isSilo = silo;
            siloName = siloId;
            siloAddress = address;
            gateway = gatewayAddress;
            this.hostName = hostName;
            if (!isSilo)
                generation = SiloAddress.AllocateNewGeneration();
        }


        Task IStatisticsPublisher.Init(
            bool isSilo,
            string storageConnectionString,
            string deploymentId,
            string address,
            string siloName,
            string hostName)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Writes statistics to the database
        /// </summary>
        /// <param name="statsCounters">Statistics counters to write</param>
        /// <returns>Task for database opearation</returns>
        public async Task ReportStats(List<ICounter> statsCounters)
        {
            var siloOrClientName = isSilo ? siloName : clientId;
            var id = isSilo
                ? siloAddress.ToLongString()
                : string.Format("{0}:{1}", siloOrClientName, generation);
            if (logger != null && logger.IsVerbose3)
                logger.Verbose3(
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
                    //The query template from which to retrieve the set of columns that are being inserted.

                    insertTasks.Add(repository.InsertStatisticsCountersAsync(
                        new OrleansStatisticsTable
                        {
                            DeploymentId = deploymentId,
                            HostName = hostName,
                            Name = siloOrClientName,
                            Id = id
                        },
                        counterBatch));

                await Task.WhenAll(insertTasks);
            }
            catch (Exception ex)
            {
                if (logger != null && logger.IsVerbose) logger.Verbose("ReportStats faulted: {0}", ex.ToString());
                foreach (var faultedTask in insertTasks.Where(t => t.IsFaulted))
                    if (logger != null && logger.IsVerbose)
                        logger.Verbose("Faulted task exception: {0}", faultedTask.ToString());

                throw;
            }

            if (logger != null && logger.IsVerbose) logger.Verbose("ReportStats SUCCESS");
        }

        /// <summary>
        ///     Name of the provider
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Initializes publisher
        /// </summary>
        /// <param name="name">Provider name</param>
        /// <param name="providerRuntime">Provider runtime API</param>
        /// <param name="config">Provider configuration</param>
        /// <returns></returns>
        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;
            logger = providerRuntime.GetLogger("MongoStatisticsPublisher");

            var connectionString = config.Properties["ConnectionString"];
            var database = string.Empty;

            if (!config.Properties.ContainsKey("Database") || string.IsNullOrEmpty(config.Properties["Database"]))
                database = MongoUrl.Create(connectionString).DatabaseName;
            else
                database = config.Properties["Database"];


            repository = new MongoStatisticsPublisherRepository(connectionString, database);
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Closes provider
        /// </summary>
        /// <returns>Resolved task</returns>
        public Task Close()
        {
            return Task.CompletedTask;
        }


        /// <summary>
        ///     Batches the counters list to batches of given maximum size.
        /// </summary>
        /// <param name="counters">The counters to batch.</param>
        /// <param name="maxBatchSizeInclusive">The maximum size of one batch.</param>
        /// <returns>The counters batched.</returns>
        private static List<List<ICounter>> BatchCounters(List<ICounter> counters, int maxBatchSizeInclusive)
        {
            var batches = new List<List<ICounter>>();
            for (var i = 0; i < counters.Count; i += maxBatchSizeInclusive)
                batches.Add(counters.GetRange(i, Math.Min(maxBatchSizeInclusive, counters.Count - i)));

            return batches;
        }
    }
}