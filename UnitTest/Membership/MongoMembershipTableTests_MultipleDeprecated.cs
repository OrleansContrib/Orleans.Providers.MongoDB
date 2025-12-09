
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Membership;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using System.Threading.Tasks;
using TestExtensions;
using UnitTests;
using UnitTests.MembershipTests;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    [TestCategory("Membership")]
    [TestCategory("Mongo")]
    public class MongoMembershipTableTests_MultipleDeprecated : MembershipTableTestsBase
    {
        private readonly ITestOutputHelper testOutputHelper;
        private MongoClientJig mongoClientFixture;

        public MongoMembershipTableTests_MultipleDeprecated(ConnectionStringFixture fixture, TestEnvironmentFixture environment, ITestOutputHelper testOutputHelper)
            : base(fixture, environment, new LoggerFilterOptions())
        {
            this.testOutputHelper = testOutputHelper;
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
                Strategy = MongoDBMembershipStrategy.MultipleDeprecated
            });

            return new MongoMembershipTable(
                mongoClientFixture.CreateDatabaseFactory(),
                loggerFactory.CreateLogger<MongoMembershipTable>(),
                _clusterOptions,
                options);
        }

        protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
        {
            var options = Options.Create(new MongoDBGatewayListProviderOptions
            {
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest",
                Strategy = MongoDBMembershipStrategy.MultipleDeprecated
            });

            return new MongoGatewayListProvider(
                mongoClientFixture.CreateDatabaseFactory(),
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
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_GetGateways()
        {
            await MembershipTable_GetGateways();
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_EmptyTable()
        {
            await MembershipTable_ReadAll_EmptyTable();
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_InsertRow()
        {
            await MembershipTable_InsertRow(false);
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadRow_Insert_Read()
        {
            await MembershipTable_ReadRow_Insert_Read(false);
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_Insert_ReadAll()
        {
            await MembershipTable_ReadAll_Insert_ReadAll(false);
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRow()
        {
            await MembershipTable_UpdateRow(false);
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRowInParallel()
        {
            await MembershipTable_UpdateRowInParallel(false);
            await mongoClientFixture.AssertQualityChecksAsync(testOutputHelper);
        }
    }
}