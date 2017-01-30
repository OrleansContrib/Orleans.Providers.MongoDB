namespace Orleans.Providers.MongoDB.Statistics.Repository
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Orleans.Runtime;

    public interface IMongoStatisticsPublisherRepository
    {
        /// <summary>
        /// Upsert client metrics.
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
        Task UpsertReportClientMetricsAsync(OrleansClientMetricsTable metricsTable, IClientPerformanceMetrics clientMetrics);

        /// <summary>
        /// Upsert silo metrics async.
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
        Task UpsertSiloMetricsAsync(OrleansSiloMetricsTable siloMetricsTable, ISiloPerformanceMetrics siloPerformanceMetrics);

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
        Task InsertStatisticsCountersAsync(OrleansStatisticsTable statisticsTable, List<ICounter> counterBatch);
    }
}
