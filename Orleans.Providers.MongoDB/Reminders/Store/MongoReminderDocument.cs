using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Reminders.Store
{
    public class MongoReminderDocument
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRequired]
        public string ServiceId { get; set; }

        [BsonRequired]
        public string GrainId { get; set; }

        [BsonRequired]
        public string ReminderName { get; set; }

        [BsonRequired]
        public double Period { get; set; }

        [BsonRequired]
        public long GrainHash { get; set; }

        [BsonRequired]
        public long Version { get; set; }

        [BsonRequired]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }
    }
}