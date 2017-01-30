using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Statistics.Repository
{
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    using InsertManyOptions = global::MongoDB.Driver.InsertManyOptions;

    /// <summary>
    /// The mongo statistics publisher repository.
    /// These functions are used to publish silo & client statistics
    /// </summary>
    public class MongoStatisticsPublisherRepository : DocumentRepository, IMongoStatisticsPublisherRepository
    {
        /// <summary>
        /// The client metrics table.
        /// </summary>
        private static readonly string ClientMetricsTableName = "OrleansClientMetricsTable";
        private static readonly string OrleansSiloMetricsTableName = "OrleansSiloMetricsTable";
        private static readonly string OrleansStatisticsTableName = "OrleansStatisticsTable";
        private static readonly string DeploymentIdName = "DeploymentId";
        private static readonly string ClientIdName = "ClientId";
        private static readonly string SiloIdName = "SiloId";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentRepository"/> class.
        /// </summary>
        /// <param name="connectionsString">
        /// The connections string.
        /// </param>
        /// <param name="databaseName">
        /// The database name.
        /// </param>
        public MongoStatisticsPublisherRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {
        }

        /// <summary>
        /// Upsert report client metrics.
        /// </summary>
        /// <param name="metricsTable">
        /// The metrics table.
        /// </param>
        /// <param name="clientMetrics">
        /// The client metrics.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task UpsertReportClientMetricsAsync(OrleansClientMetricsTable metricsTable, IClientPerformanceMetrics clientMetrics)
        {
            metricsTable.ClientCount = clientMetrics.ConnectedGatewayCount;
            metricsTable.MemoryUsage = clientMetrics.AvailablePhysicalMemory;
            metricsTable.CpuUsage = clientMetrics.CpuUsage;
            metricsTable.MemoryUsage = clientMetrics.MemoryUsage;
            metricsTable.RequestQueueLength = clientMetrics.ReceiveQueueLength;
            metricsTable.ReceivedMessages = clientMetrics.ReceivedMessages;
            metricsTable.SendQueueLength = clientMetrics.SendQueueLength;
            metricsTable.SentMessages = clientMetrics.SentMessages;

            var collection = this.ReturnOrCreateCollection(ClientMetricsTableName);

            FilterDefinition<BsonDocument> filter = null;
            BsonDocument document = metricsTable.ToBsonDocument();
            filter = Builders<BsonDocument>.Filter.Eq(DeploymentIdName, metricsTable.DeploymentId) & Builders<BsonDocument>.Filter.Eq(ClientIdName, metricsTable.ClientId);

            await collection.ReplaceOneAsync(
                 filter,
                 document,
                 new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true });

        }

        /// <summary>
        /// Upsert silo metrics.
        /// </summary>
        /// <param name="siloMetricsTable">
        /// The silo metrics table.
        /// </param>
        /// <param name="siloPerformanceMetrics">
        /// The silo performance metrics.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task UpsertSiloMetricsAsync(OrleansSiloMetricsTable siloMetricsTable, ISiloPerformanceMetrics siloPerformanceMetrics)
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

            var collection = this.ReturnOrCreateCollection(OrleansSiloMetricsTableName);

            FilterDefinition<BsonDocument> filter = null;
            BsonDocument document = siloMetricsTable.ToBsonDocument();
            filter = Builders<BsonDocument>.Filter.Eq(DeploymentIdName, siloMetricsTable.DeploymentId) & Builders<BsonDocument>.Filter.Eq(SiloIdName, siloMetricsTable.SiloId);

            await collection.ReplaceOneAsync(
                 filter,
                 document,
                 new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true });
        }

        /// <summary>
        /// Insert statistics counters.
        /// </summary>
        /// <param name="statisticsTable">
        /// The statistics table.
        /// </param>
        /// <param name="counterBatch">
        /// The counter batch.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task InsertStatisticsCountersAsync(
            OrleansStatisticsTable statisticsTable,
            List<ICounter> counterBatch)
        {
            List<BsonDocument> documents = new List<BsonDocument>();

            OrleansStatisticsTable newStatisticTable = null;

            foreach (ICounter counter in counterBatch)
            {
                newStatisticTable = new OrleansStatisticsTable
                                        {
                                            DeploymentId = statisticsTable.DeploymentId,
                                            HostName = statisticsTable.HostName,
                                            Name = statisticsTable.Name,
                                            Id = statisticsTable.Id
                                        };

                newStatisticTable.IsValueDelta = counter.IsValueDelta;
                newStatisticTable.StatValue = counter.GetValueString();
                newStatisticTable.Statistic = counter.GetDisplayString();

                documents.Add(newStatisticTable.ToBsonDocument());
            }

            var collection = this.ReturnOrCreateCollection(OrleansStatisticsTableName);
            await collection.InsertManyAsync(documents, new InsertManyOptions { BypassDocumentValidation = true });
        }

    }
}
