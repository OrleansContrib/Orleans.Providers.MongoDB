using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Orleans.Providers.MongoDB.Membership.Store.Multiple
{
    public sealed class TableVersionDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string DeploymentId { get; set; }

        [BsonRequired]
        public int Version { get; set; }

        [BsonRequired]
        public string VersionEtag { get; set; }

        public static TableVersionDocument Create(string deploymentId, TableVersion tableVersion)
        {
            return new TableVersionDocument
            {
                DeploymentId = deploymentId,
                Version = tableVersion.Version,
                VersionEtag = Guid.NewGuid().ToString()
            };
        }

        public TableVersion ToTableVersion()
        {
            return new TableVersion(Version, VersionEtag);
        }
    }
}
