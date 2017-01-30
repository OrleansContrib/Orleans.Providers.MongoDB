namespace Orleans.Providers.MongoDB.Statistics
{
    using System;

    using global::MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// The orleans statistics table.
    /// </summary>
    public class OrleansStatisticsTable
    {
        public string DeploymentId { get; set; }

        public string Id { get; set; }

        public string HostName { get; set; }

        public string Name { get; set; }

        public bool IsValueDelta { get; set; }

        public string StatValue { get; set; }

        public string Statistic { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrleansStatisticsTable"/> class.
        /// </summary>
        public OrleansStatisticsTable()
        {
            this.Timestamp = DateTime.Now;
        }
    }
}
