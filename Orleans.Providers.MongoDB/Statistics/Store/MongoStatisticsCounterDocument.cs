using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Statistics
{
    public sealed class MongoStatisticsCounterDocument
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRequired]
        public string Identity { get; set; }

        [BsonRequired]
        public string DeploymentId { get; set; }

        [BsonRequired]
        public string HostName { get; set; }

        [BsonRequired]
        public string Name { get; set; }

        [BsonRequired]
        public bool IsValueDelta { get; set; }

        [BsonRequired]
        public string StatValue { get; set; }

        [BsonRequired]
        public string Statistic { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Timestamp { get; set; }
    }
}