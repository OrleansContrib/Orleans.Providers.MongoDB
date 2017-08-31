using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Statistics
{
    /// <summary>
    ///     The Client Metrics Model
    /// </summary>
    public class OrleansClientMetricsTable
    {
        public OrleansClientMetricsTable()
        {
            DateTime = DateTime.Now;
        }

        public string DeploymentId { get; set; }

        public string ClientId { get; set; }

        public string Address { get; set; }

        public string HostName { get; set; }

        public float CpuUsage { get; set; }

        public long MemoryUsage { get; set; }

        public int SendQueueLength { get; set; }

        public int ReceiveQueueLength { get; set; }

        public long SentMessages { get; set; }

        public long ReceivedMessages { get; set; }

        public long ConnectedGateWayCount { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DateTime { get; set; }
    }
}