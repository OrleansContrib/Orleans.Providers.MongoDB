namespace Orleans.Providers.MongoDB.Membership
{
    #region Using

    using System;

    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization.Attributes;

    #endregion

    /// <summary>
    /// The membership table.
    /// </summary>
    public class MembershipTable
    {
        public string Address { get; set; }

        public string DeploymentId { get; set; }

        public int Generation { get; set; }

        public string HostName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime IAmAliveTime { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Port { get; set; }

        public int ProxyPort { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }

        public int Status { get; set; }

        public string SuspectTimes { get; set; }

    }
}