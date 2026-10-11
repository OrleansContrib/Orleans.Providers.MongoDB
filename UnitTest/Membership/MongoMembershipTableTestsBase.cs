using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Clustering.TestKit;
using Orleans.Configuration;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Membership;
using Orleans.Providers.MongoDB.Utils;
using TestExtensions;
using UnitTests;
using UnitTests.MembershipTests;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    [TestCategory("Membership")]
    [TestCategory("Mongo")]
    public abstract class MongoMembershipTableTestsBase : MembershipTableTestsBase
    {
        private const string DatabaseName = "OrleansTest";
        private const string CollectionPrefix = "Test_";
        private IMongoDatabase probeDatabase;

        protected MongoMembershipTableTestsBase(ConnectionStringFixture fixture, TestEnvironmentFixture environment)
            : base(fixture, environment, new LoggerFilterOptions())
        {
        }

        protected abstract MongoDBMembershipStrategy Strategy { get; }

        protected abstract IMongoClientFactory ClientFactory { get; }

        protected override IMembershipTable CreateMembershipTable(ILogger logger)
        {
            return CreateMembershipTable(logger, _clusterOptions);
        }

        protected override IMembershipTable CreateMembershipTable(ILogger logger, IOptions<ClusterOptions> clusterOptions)
        {
            var options = Options.Create(new MongoDBMembershipTableOptions
            {
                CollectionPrefix = CollectionPrefix,
                DatabaseName = DatabaseName,
                Strategy = Strategy
            });

            return new MongoMembershipTable(
                ClientFactory,
                loggerFactory.CreateLogger<MongoMembershipTable>(),
                clusterOptions,
                options);
        }

        protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
        {
            var options = Options.Create(new MongoDBGatewayListProviderOptions
            {
                CollectionPrefix = CollectionPrefix,
                DatabaseName = DatabaseName,
                Strategy = Strategy
            });

            return new MongoGatewayListProvider(
                ClientFactory,
                loggerFactory.CreateLogger<MongoGatewayListProvider>(),
                _clusterOptions,
                _gatewayOptions,
                options);
        }

        protected override MembershipTableTestFixture CreateConformanceFixture()
        {
            return CreateConformanceFixture(IsClusterDeletedAsync);
        }

        /// <summary>
        /// Read-only probe used by the clustering TestKit to verify that a cluster's membership data has been deleted.
        /// </summary>
        private async ValueTask<bool> IsClusterDeletedAsync(string clusterId, CancellationToken cancellationToken)
        {
            var database = probeDatabase ??= ClientFactory.Create("Probe").GetDatabase(DatabaseName);

            switch (Strategy)
            {
                case MongoDBMembershipStrategy.SingleDocument:
                    return !await ExistsAsync(database, "OrleansMembershipSingle", "_id", clusterId, cancellationToken);
                case MongoDBMembershipStrategy.Multiple:
                    return
                        !await ExistsAsync(database, "OrleansMembershipV3", "DeploymentId", clusterId, cancellationToken) &&
                        !await ExistsAsync(database, "OrleansMembershipV3_TableVersion", "_id", clusterId, cancellationToken);
                case MongoDBMembershipStrategy.MultipleDeprecated:
                    return !await ExistsAsync(database, "OrleansMembershipV2", "DeploymentId", clusterId, cancellationToken);
            }

            throw new ArgumentOutOfRangeException(nameof(Strategy), Strategy, null);
        }

        private static async Task<bool> ExistsAsync(IMongoDatabase database, string collectionName, string field, string clusterId, CancellationToken cancellationToken)
        {
            var collection = database.GetCollection<BsonDocument>(CollectionPrefix + collectionName);

            return await collection
                .Find(Builders<BsonDocument>.Filter.Eq(field, clusterId))
                .Limit(1)
                .AnyAsync(cancellationToken);
        }
    }
}
