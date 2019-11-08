using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Membership.Store.Multiple;
using Orleans.Providers.MongoDB.Membership.Store.MultipleDeprecated;
using Orleans.Providers.MongoDB.Membership.Store.Single;
using System;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public static class Factory
    {
        public static IMongoMembershipCollection CreateCollection(MongoDBOptions options, MongoDBMembershipStrategy strategy)
        {
            switch (strategy)
            {
                case MongoDBMembershipStrategy.SingleDocument:
                     return new SingleMembershipCollection(
                        options.ConnectionString,
                        options.DatabaseName,
                        options.CollectionPrefix,
                        options.CreateShardKeyForCosmos);
                case MongoDBMembershipStrategy.Muiltiple:
                    return new MultipleMembershipCollection(
                        options.ConnectionString,
                        options.DatabaseName,
                        options.CollectionPrefix,
                        options.CreateShardKeyForCosmos);
                case MongoDBMembershipStrategy.MultipleDeprecated:
                    return new MultipleDeprecatedMembershipCollection(
                        options.ConnectionString,
                        options.DatabaseName,
                        options.CollectionPrefix,
                        options.CreateShardKeyForCosmos);
            }

            throw new ArgumentException("Invalid strategy.", nameof(strategy));
        }
    }
}
