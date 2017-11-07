using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Membership
{
    /// <summary>
    ///     The membership table.
    /// </summary>
    public class MembershipCollection
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Address { get; set; }

        public string DeploymentId { get; set; }

        public int Generation { get; set; }

        public string HostName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime IAmAliveTime { get; set; }

        public int Port { get; set; }

        public int ProxyPort { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }

        public int Status { get; set; }

        public string SuspectTimes { get; set; }
    }
}