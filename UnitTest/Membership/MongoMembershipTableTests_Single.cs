using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Providers.MongoDB.Utils;
using TestExtensions;
using UnitTests;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    public class MongoMembershipTableTests_Single : MongoMembershipTableTestsBase
    {
        public MongoMembershipTableTests_Single(ConnectionStringFixture fixture, TestEnvironmentFixture environment)
            : base(fixture, environment)
        {
        }

        protected override MongoDBMembershipStrategy Strategy => MongoDBMembershipStrategy.SingleDocument;

        protected override IMongoClientFactory ClientFactory => MongoDatabaseFixture.DatabaseFactory;

        protected override Task<string> GetConnectionString()
        {
            return Task.FromResult(MongoDatabaseFixture.DatabaseConnectionString);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_CleanupDefunctSiloEntries()
        {
            await MembershipTable_CleanupDefunctSiloEntries();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_GetGateways()
        {
            await MembershipTable_GetGateways();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_EmptyTable()
        {
            await MembershipTable_ReadAll_EmptyTable();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_InsertRow()
        {
            await MembershipTable_InsertRow();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadRow_Insert_Read()
        {
            await MembershipTable_ReadRow_Insert_Read();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_Insert_ReadAll()
        {
            await MembershipTable_ReadAll_Insert_ReadAll();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRow()
        {
            await MembershipTable_UpdateRow();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRowInParallel()
        {
            await MembershipTable_UpdateRowInParallel();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateIAmAlive()
        {
            await MembershipTable_UpdateIAmAlive();
        }
    }
}
