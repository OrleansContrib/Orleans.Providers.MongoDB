using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Membership
{
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization.Attributes;

    public class MembershipTable
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string DeploymentId { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public int Generation { get; set; }

        public string HostName { get; set; }

        public int Status { get; set; }

        public int ProxyPort{ get; set; }

        public string SuspectTimes { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime IAmAliveTime { get; set; }
    }
}
