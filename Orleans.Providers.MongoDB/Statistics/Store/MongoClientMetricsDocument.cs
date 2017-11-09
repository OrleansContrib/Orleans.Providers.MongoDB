using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Statistics.Store
{
    public sealed class MongoClientMetricsDocument
    {
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        [BsonRequired]
        public string DeploymentId { get; set; }

        [BsonRequired]
        public string ClientId { get; set; }

        [BsonRequired]
        public string Address { get; set; }

        [BsonRequired]
        public string HostName { get; set; }

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
        public long ConnectedGateWayCount { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Timestamp { get; set; }
    }
}