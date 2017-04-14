namespace Orleans.Providers.MongoDB.Statistics
{
    using System;

    using global::MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// The Client Metrics Model
    /// </summary>
    public class OrleansClientMetricsTable
    {
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

        public OrleansClientMetricsTable()
        {
            this.DateTime = DateTime.Now;
        }

    }
}
