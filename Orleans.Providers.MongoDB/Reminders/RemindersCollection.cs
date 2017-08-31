using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Reminders
{
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