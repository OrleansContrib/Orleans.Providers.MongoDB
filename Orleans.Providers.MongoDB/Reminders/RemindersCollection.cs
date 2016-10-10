using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Reminders
{
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization.Attributes;

    public class RemindersCollection
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ServiceId { get; set; }

        public string GrainId { get; set; }

        public string ReminderName { get; set; }

        public double Period { get; set; }

        public long GrainHash { get; set; }

        public long Version { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }

    }
}
