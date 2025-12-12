using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Reminders.Store
{
    public class MongoReminderDocument
    {
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        [BsonRequired]
        public string ServiceId { get; set; }

        [BsonRequired]
        public string GrainId { get; set; }

        [BsonRequired]
        public string ReminderName { get; set; }

        [BsonRequired]
        public string Etag { get; set; }

        [BsonRequired]
        public TimeSpan Period { get; set; }

        [BsonRequired]
        public long GrainHash { get; set; }

        [BsonRequired]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartAt { get; set; }

        public static MongoReminderDocument Create(string id, string serviceId, ReminderEntry entry, string etag)
        {
            if (entry.StartAt.Kind is DateTimeKind.Unspecified)
            {
                entry.StartAt = new DateTime(entry.StartAt.Ticks, DateTimeKind.Utc);
            }

            return new MongoReminderDocument
            {
                Id = id,
                Etag = etag,
                GrainHash = entry.GrainId.GetUniformHashCode(),
                GrainId = entry.GrainId.ToString(),
                Period = entry.Period,
                ReminderName = entry.ReminderName,
                ServiceId = serviceId,
                StartAt = entry.StartAt
            };
        }

        public ReminderEntry ToEntry()
        {
            return new ReminderEntry
            {
                ETag = Etag,
                GrainId = Runtime.GrainId.Parse(GrainId),
                Period = Period,
                ReminderName = ReminderName,
                StartAt = StartAt
            };
        }
    }
}
