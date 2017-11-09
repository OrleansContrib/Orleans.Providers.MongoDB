using System.Net;
using MongoDB.Bson.Serialization.Attributes;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public sealed class MongoMembershipAddress
    {
        [BsonRequired]
        public string Address { get; set; }

        [BsonRequired]
        public int Generation { get; set; }

        [BsonRequired]
        public int Port { get; set; }

        public static MongoMembershipAddress Create(SiloAddress address)
        {
            var ip4 = address.Endpoint.Address.MapToIPv4().ToString();

            return new MongoMembershipAddress { Address = ip4, Port = address.Endpoint.Port, Generation = address.Generation };
        }

        public SiloAddress ToSiloAddress()
        {
            return SiloAddress.New(new IPEndPoint(IPAddress.Parse(Address), Port), Generation);
        }
    }
}
