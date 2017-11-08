using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Statistics.Store
{
    public sealed class MongoSiloMetricsDocument
    {
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        [BsonRequired]
        public string DeploymentId { get; set; }

        [BsonRequired]
        public string SiloId { get; set; }

        [BsonRequired]
        public string Address { get; set; }

        [BsonRequired]
        public int Port { get; set; }

        [BsonRequired]
        public long Generation { get; set; }

        [BsonRequired]
        public string HostName { get; set; }

        [BsonRequired]
        public string GatewayAddress { get; set; }

        [BsonRequired]
        public int GatewayPort { get; set; }

        [BsonRequired]
        public float CpuUsage { get; set; }

        [BsonRequired]
        public long MemoryUsage { get; set; }

        [BsonRequired]
        public int SendQueueLength { get; set; }

        [BsonRequired]
        public int ReceiveQueueLength { get; set; }

        [BsonRequired]
        public long SentMessages { get; set; }

        [BsonRequired]
        public long ReceivedMessages { get; set; }

        [BsonRequired]
        public int ActivationCount { get; set; }

        [BsonRequired]
        public int RecentlyUsedActivationCount { get; set; }

        [BsonRequired]
        public long RequestQueueLength { get; set; }

        [BsonRequired]
        public bool IsOverloaded { get; set; }

        [BsonRequired]
        public long ClientCount { get; set; }

        [BsonRequired]
        public long AvailablePhysicalMemory { get; set; }

        [BsonRequired]
        public long TotalPhysicalMemory { get; set; }

        [BsonRequired]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime TimeStamp { get; set; }

    }
}