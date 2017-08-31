using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Statistics
{
    /// <summary>
    ///     The orleans statistics table.
    /// </summary>
    public class OrleansStatisticsTable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OrleansStatisticsTable" /> class.
        /// </summary>
        public OrleansStatisticsTable()
        {
            Timestamp = DateTime.Now;
            Id = ObjectId.GenerateNewId().ToString();
        }

        public string Identity { get; set; }

        public string DeploymentId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string HostName { get; set; }

        public string Name { get; set; }

        public bool IsValueDelta { get; set; }

        public string StatValue { get; set; }

        public string Statistic { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Timestamp { get; set; }
    }
}