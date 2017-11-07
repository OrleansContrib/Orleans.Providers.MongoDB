using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Repository
{
    public class MongoSiloMetricsRepository : DocumentRepository2<OrleansSiloMetricsTable>
    {
        private static readonly UpdateOptions UpsertNoValidation = new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true };
        private readonly TimeSpan? expireAfter;

        public MongoSiloMetricsRepository(string connectionString, string databaseName, TimeSpan? expireAfter)
            : base(connectionString, databaseName)
        {
            this.expireAfter = expireAfter;
        }

        protected override string CollectionName()
        {
            return "OrleansSiloMetricsTable";
        }

        protected override Task SetupCollectionAsync(IMongoCollection<OrleansSiloMetricsTable> collection)
        {
            if (!expireAfter.HasValue)
            {
                return collection.Indexes.CreateOneAsync(Index.Ascending(x => x.TimeStamp), 
                    new CreateIndexOptions { ExpireAfter = expireAfter });
            }
            else
            {
                return collection.Indexes.CreateOneAsync(Index.Ascending(x => x.DeploymentId).Ascending(x => x.SiloId), 
                    new CreateIndexOptions { Unique = true });
            }
        }

        public Task UpsertSiloMetricsAsync(OrleansSiloMetricsTable siloMetricsTable, ISiloPerformanceMetrics siloPerformanceMetrics)
        {
            siloMetricsTable.ActivationCount = siloPerformanceMetrics.ActivationCount;
            siloMetricsTable.ClientCount = siloPerformanceMetrics.ClientCount;
            siloMetricsTable.IsOverloaded = siloPerformanceMetrics.IsOverloaded;
            siloMetricsTable.RecentlyUsedActivationCount = siloPerformanceMetrics.RecentlyUsedActivationCount;
            siloMetricsTable.RequestQueueLength = siloPerformanceMetrics.RequestQueueLength;
            siloMetricsTable.MemoryUsage = siloPerformanceMetrics.MemoryUsage;
            siloMetricsTable.AvailablePhysicalMemory = siloPerformanceMetrics.AvailablePhysicalMemory;
            siloMetricsTable.CpuUsage = siloPerformanceMetrics.CpuUsage;
            siloMetricsTable.ReceiveQueueLength = siloPerformanceMetrics.ReceiveQueueLength;
            siloMetricsTable.ReceivedMessages = siloPerformanceMetrics.ReceivedMessages;
            siloMetricsTable.SendQueueLength = siloPerformanceMetrics.SendQueueLength;
            siloMetricsTable.SentMessages = siloPerformanceMetrics.SentMessages;
            siloMetricsTable.TotalPhysicalMemory = siloPerformanceMetrics.TotalPhysicalMemory;

            if (this.expireAfter.HasValue)
            {
                return Collection.InsertOneAsync(siloMetricsTable);
            }
            else
            {
                return Collection.ReplaceOneAsync(x =>
                        x.DeploymentId == siloMetricsTable.DeploymentId &&
                        x.SiloId == siloMetricsTable.SiloId,
                    siloMetricsTable,
                    UpsertNoValidation);
            }
        }
    }
}
