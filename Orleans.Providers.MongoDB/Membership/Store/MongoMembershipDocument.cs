using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public sealed class MongoMembershipDocument : MembershipBase
    {
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        [BsonRequired]
        public string DeploymentId { get; set; }

        public static MongoMembershipDocument Create(MembershipEntry entry, string deploymentId, string id)
        {
            var result = Create<MongoMembershipDocument>(entry);

            result.Id = id;
            result.DeploymentId = deploymentId;

            return result;
        }
    }
}