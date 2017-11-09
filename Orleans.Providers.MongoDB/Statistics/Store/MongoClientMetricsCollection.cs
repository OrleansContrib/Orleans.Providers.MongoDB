using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Store
{
    public class MongoClientMetricsCollection : CollectionBase<MongoClientMetricsDocument>
    {
        private static readonly UpdateOptions UpsertNoValidation = new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true };
        private readonly TimeSpan? expireAfter;

        public MongoClientMetricsCollection(string connectionString, string databaseName, TimeSpan? expireAfter)
            : base(connectionString, databaseName)
        {
            this.expireAfter = expireAfter;
        }

        protected override string CollectionName()
        {
            return "OrleansClientMetricsTable";
        }

        protected override void SetupCollection(IMongoCollection<MongoClientMetricsDocument> collection)
        {
            if (!expireAfter.HasValue)
            {
                collection.Indexes.CreateOne(Index.Ascending(x => x.Timestamp), new CreateIndexOptions { ExpireAfter = expireAfter });
            }
        }

        public virtual Task UpsertReportClientMetricsAsync(
            string deploymentId, 
            string clientId, 
            string address, 
            string hostName, 
            IClientPerformanceMetrics clientMetrics)
        {
            var id = ReturnId(deploymentId, clientId, expireAfter.HasValue);

            var document = new MongoClientMetricsDocument
            {
                Id = id,
                Address = address,
                ClientId = clientId,
                ConnectedGateWayCount = clientMetrics.ConnectedGatewayCount,
                CpuUsage = clientMetrics.CpuUsage,
                DeploymentId = deploymentId,
                HostName = hostName,
                MemoryUsage = clientMetrics.MemoryUsage,
                ReceivedMessages = clientMetrics.ReceivedMessages,
                SendQueueLength = clientMetrics.SendQueueLength,
                SentMessages = clientMetrics.SentMessages,
                Timestamp = DateTime.UtcNow
            };

            if (expireAfter.HasValue)
            {
                return Collection.InsertOneAsync(document);
            }
            else
            {
                return Collection.ReplaceOneAsync(x => x.Id == id, document, UpsertNoValidation);
            }
        }

        private static string ReturnId(string deploymentId, string clientId, bool multiple)
        {
            var id =  $"{deploymentId}:{clientId}";

            if (multiple)
            {
                id += $"{DateTime.UtcNow:yyyy-MM-dd_hh:mm:ss}";
            }

            return id;
        }
    }
}
