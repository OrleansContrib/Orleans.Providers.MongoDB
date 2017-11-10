using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Store
{
    public class MongoSiloMetricsCollection : CollectionBase<MongoSiloMetricsDocument>
    {
        private static readonly UpdateOptions UpsertNoValidation = new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true };
        private readonly TimeSpan expireAfter;
        private readonly string collectionPrefix;

        public MongoSiloMetricsCollection(string connectionString, string databaseName, TimeSpan expireAfter, string collectionPrefix)
            : base(connectionString, databaseName)
        {
            this.expireAfter = expireAfter;
            this.collectionPrefix = collectionPrefix;
        }

        protected override string CollectionName()
        {
            return collectionPrefix + "OrleansSiloMetricsTable";
        }

        protected override void SetupCollection(IMongoCollection<MongoSiloMetricsDocument> collection)
        {
            if (expireAfter != TimeSpan.Zero)
            {
                collection.Indexes.CreateOne(Index.Ascending(x => x.TimeStamp), new CreateIndexOptions { ExpireAfter = expireAfter });
            }
        }

        public virtual async Task UpsertSiloMetricsAsync(
            string deploymentId,
            string siloId,
            string siloAddress,
            string gatewayAddress,
            string hostName,
            int generation,
            ISiloPerformanceMetrics siloPerformanceMetrics)
        {
            var id = ReturnId(deploymentId, siloId, expireAfter != TimeSpan.Zero);

            var siloMetricsTable = new MongoSiloMetricsDocument
            {
                Id = id,
                TimeStamp = DateTime.UtcNow,
                ActivationCount = siloPerformanceMetrics.ActivationCount,
                SiloAddress = siloAddress,
                AvailablePhysicalMemory = siloPerformanceMetrics.AvailablePhysicalMemory,
                ClientCount = siloPerformanceMetrics.ClientCount,
                CpuUsage = siloPerformanceMetrics.CpuUsage,
                DeploymentId = deploymentId,
                GatewayAddress = gatewayAddress,
                Generation = generation,
                HostName = hostName,
                IsOverloaded = siloPerformanceMetrics.IsOverloaded,
                MemoryUsage = siloPerformanceMetrics.MemoryUsage,
                ReceivedMessages = siloPerformanceMetrics.ReceivedMessages,
                ReceiveQueueLength = siloPerformanceMetrics.ReceiveQueueLength,
                RecentlyUsedActivationCount = siloPerformanceMetrics.RecentlyUsedActivationCount,
                RequestQueueLength = siloPerformanceMetrics.RequestQueueLength,
                SendQueueLength = siloPerformanceMetrics.SendQueueLength,
                SentMessages = siloPerformanceMetrics.SentMessages,
                SiloId = siloId,
                TotalPhysicalMemory = siloPerformanceMetrics.TotalPhysicalMemory
            };

            try
            {
                if (expireAfter != TimeSpan.Zero)
                {
                    await Collection.InsertOneAsync(siloMetricsTable);
                }
                else
                {
                    await Collection.ReplaceOneAsync(x => x.Id == id, siloMetricsTable, UpsertNoValidation);
                }
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError.Category != ServerErrorCategory.DuplicateKey)
                {
                    throw;
                }
            }
        }

        private static string ReturnId(string deploymentId, string siloId, bool multiple)
        {
            var id = $"{deploymentId}:{siloId}";

            if (multiple)
            {
                id += Guid.NewGuid();
            }

            return id;
        }
    }
}
