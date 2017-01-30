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

        public int Port { get; set; }

        public long Generation { get; set; }

        public string HostName { get; set; }

        public string GatewayAddress { get; set; }

        public int GatewayPort { get; set; }

        public float CpuUsage { get; set; }

        public long MemoryUsage { get; set; }

        public int SendQueueLength { get; set; }

        public int ReceiveQueueLength { get; set; }

        public long SentMessages { get; set; }

        public long ReceivedMessages { get; set; }

        public int ActivationCount { get; set; }

        public int RecentlyUsedActivationCount { get; set; }

        public long RequestQueueLength { get; set; }

        public bool IsOverloaded { get; set; }

        public long ClientCount { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DateTime { get; set; }

        public long TotalMemoryUsage { get; set; }

        public OrleansClientMetricsTable()
        {
            this.DateTime = DateTime.Now;
        }

    }
}
