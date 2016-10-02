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
        #region Properties

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the deployment id.
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        /// Gets or sets the generation.
        /// </summary>
        public int Generation { get; set; }

        /// <summary>
        /// Gets or sets the host name.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the i am alive time.
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime IAmAliveTime { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the proxy port.
        /// </summary>
        public int ProxyPort { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the suspect times.
        /// </summary>
        public string SuspectTimes { get; set; }

        #endregion
    }
}