using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Store
{
    public class MongoSiloMetricsCollection : CollectionBase<MongoSiloMetricsDocument>
    {
        private static readonly UpdateOptions UpsertNoValidation = new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true };
        private readonly TimeSpan? expireAfter;

        public MongoSiloMetricsCollection(string connectionString, string databaseName, TimeSpan? expireAfter)
            : base(connectionString, databaseName)
        {
            this.expireAfter = expireAfter;
        }

        protected override string CollectionName()
        {
            return "OrleansSiloMetricsTable";
        }

        protected override void SetupCollection(IMongoCollection<MongoSiloMetricsDocument> collection)
        {
            if (!expireAfter.HasValue)
            {
                collection.Indexes.CreateOne(Index.Ascending(x => x.TimeStamp), new CreateIndexOptions { ExpireAfter = expireAfter });
            }
        }

        public Task UpsertSiloMetricsAsync(
            string deploymentId,
            string siloId,
            string siloAddress, int siloPort,
            string gatewayAddress, int gatewayPort,
            string hostName,
            int generation,
            ISiloPerformanceMetrics siloPerformanceMetrics)
        {
            var id = ReturnId(deploymentId, siloId, expireAfter.HasValue);

            var siloMetricsTable = new MongoSiloMetricsDocument
            {
                Id = id,
                TimeStamp = DateTime.UtcNow,
                ActivationCount = siloPerformanceMetrics.ActivationCount,
                Address = siloAddress,
                AvailablePhysicalMemory = siloPerformanceMetrics.AvailablePhysicalMemory,
                ClientCount = siloPerformanceMetrics.ClientCount,
                CpuUsage = siloPerformanceMetrics.CpuUsage,
                DeploymentId = deploymentId,
                GatewayAddress = gatewayAddress,
                GatewayPort = gatewayPort,
                Generation = generation,
                HostName = hostName,
                IsOverloaded = siloPerformanceMetrics.IsOverloaded,
                MemoryUsage = siloPerformanceMetrics.MemoryUsage,
                Port = siloPort,
                ReceivedMessages = siloPerformanceMetrics.ReceivedMessages,
                ReceiveQueueLength = siloPerformanceMetrics.ReceiveQueueLength,
                RecentlyUsedActivationCount = siloPerformanceMetrics.RecentlyUsedActivationCount,
                RequestQueueLength = siloPerformanceMetrics.RequestQueueLength,
                SendQueueLength = siloPerformanceMetrics.SendQueueLength,
                SentMessages = siloPerformanceMetrics.SentMessages,
                SiloId = siloId,
                TotalPhysicalMemory = siloPerformanceMetrics.TotalPhysicalMemory
            };

            if (expireAfter.HasValue)
            {
                return Collection.InsertOneAsync(siloMetricsTable);
            }
            else
            {
                return Collection.ReplaceOneAsync(x => x.Id == id, siloMetricsTable, UpsertNoValidation);
            }
        }

        private static string ReturnId(string deploymentId, string siloId, bool multiple)
        {
            var id = $"{deploymentId}:{siloId}";

            if (multiple)
            {
                id += $"{DateTime.UtcNow:yyyy-MM-dd_hh:mm:ss}";
            }

            return id;
        }
    }
}
