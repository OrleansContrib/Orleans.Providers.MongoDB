using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Repository
{
    public class MongoClientMetricsRepository : DocumentRepository2<OrleansClientMetricsTable>
    {
        private static readonly UpdateOptions UpsertNoValidation = new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true };
        private readonly TimeSpan? expireAfter;

        public MongoClientMetricsRepository(string connectionString, string databaseName, TimeSpan? expireAfter)
            : base(connectionString, databaseName)
        {
            this.expireAfter = expireAfter;
        }

        protected override string CollectionName()
        {
            return "OrleansClientMetricsTable";
        }

        protected override Task SetupCollectionAsync(IMongoCollection<OrleansClientMetricsTable> collection)
        {
            if (!expireAfter.HasValue)
            {
                return collection.Indexes.CreateOneAsync(Index.Ascending(x => x.DateTime), 
                    new CreateIndexOptions { ExpireAfter = expireAfter });
            }
            else
            {
                return collection.Indexes.CreateOneAsync(Index.Ascending(x => x.DeploymentId).Ascending(x => x.ClientId), 
                    new CreateIndexOptions { Unique = true });
            }
        }

        public virtual Task UpsertReportClientMetricsAsync(OrleansClientMetricsTable metricsTable, IClientPerformanceMetrics clientMetrics)
        {
            metricsTable.CpuUsage = clientMetrics.CpuUsage;
            metricsTable.MemoryUsage = clientMetrics.MemoryUsage;
            metricsTable.ReceivedMessages = clientMetrics.ReceivedMessages;
            metricsTable.SendQueueLength = clientMetrics.SendQueueLength;
            metricsTable.SentMessages = clientMetrics.SentMessages;
            metricsTable.ConnectedGateWayCount = clientMetrics.ConnectedGatewayCount;

            if (this.expireAfter.HasValue)
            {
                return Collection.InsertOneAsync(metricsTable);
            }
            else
            {
                return Collection.ReplaceOneAsync(x =>
                        x.DeploymentId == metricsTable.DeploymentId &&
                        x.ClientId == metricsTable.ClientId,
                    metricsTable,
                    UpsertNoValidation);
            }
        }
    }
}
