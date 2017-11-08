using System;
using MongoDB.Bson.Serialization.Attributes;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public sealed class MongoSuspectTime
    {
        [BsonRequired]
        public MongoMembershipAddress Address { get; set; }

        [BsonRequired]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastTime { get; set; }

        public static MongoSuspectTime Create(Tuple<SiloAddress, DateTime> tuple)
        {
            return new MongoSuspectTime { Address = MongoMembershipAddress.Create(tuple.Item1), LastTime = tuple.Item2 };
        }

        public Tuple<SiloAddress, DateTime> ToTuple()
        {
            return Tuple.Create(Address.ToSiloAddress(), LastTime);
        }
    }
}
