using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Membership;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using TestExtensions;
using UnitTests;
using UnitTests.MembershipTests;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    [TestCategory("Membership")]
    [TestCategory("Mongo")]
    public class MongoMembershipTableTests_Single : MembershipTableTestsBase
    {
        private MongoClientJig mongoClientFixture;

        public MongoMembershipTableTests_Single(ConnectionStringFixture fixture, TestEnvironmentFixture environment)
            : base(fixture, environment, new LoggerFilterOptions())
        {
        }

        protected override IMembershipTable CreateMembershipTable(ILogger logger)
        {
            // the virtual method is called from the base constructor, which means there wasn't a chance to
            // initialize the field via the typical constructor
            mongoClientFixture ??= new MongoClientJig();
            
            var options = Options.Create(new MongoDBMembershipTableOptions
            {
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest",
                Strategy = MongoDBMembershipStrategy.SingleDocument
            });

            return new MongoMembershipTable(
                mongoClientFixture.CreateReplicaSetFactory(),
                loggerFactory.CreateLogger<MongoMembershipTable>(),
                _clusterOptions,
                options);
        }

        protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
        {
            // the virtual method is called from the base constructor, which means there wasn't a chance to
            // initialize the field via the typical constructor
            mongoClientFixture ??= new MongoClientJig();
            
            var options = Options.Create(new MongoDBGatewayListProviderOptions
            {
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest",
                Strategy = MongoDBMembershipStrategy.SingleDocument
            });

            return new MongoGatewayListProvider(
                mongoClientFixture.CreateReplicaSetFactory(),
                loggerFactory.CreateLogger<MongoGatewayListProvider>(),
                _clusterOptions,
                _gatewayOptions,
                options);
        }

        protected override Task<string> GetConnectionString()
        {
            return Task.FromResult(MongoDatabaseFixture.DatabaseConnectionString);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_CleanupDefunctSiloEntries()
        {
            await MembershipTable_CleanupDefunctSiloEntries();
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_GetGateways()
        {
            await MembershipTable_GetGateways();
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_EmptyTable()
        {
            await MembershipTable_ReadAll_EmptyTable();
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_InsertRow()
        {
            await MembershipTable_InsertRow(true);
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadRow_Insert_Read()
        {
            await MembershipTable_ReadRow_Insert_Read(true);
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_Insert_ReadAll()
        {
            await MembershipTable_ReadAll_Insert_ReadAll(true);
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRow()
        {
            await MembershipTable_UpdateRow(true);
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRowInParallel()
        {
            await MembershipTable_UpdateRowInParallel(true);
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateIAmAlive()
        {
            await MembershipTable_UpdateIAmAlive(true);
            await mongoClientFixture.AssertQualityChecksAsync();
        }
    }
}