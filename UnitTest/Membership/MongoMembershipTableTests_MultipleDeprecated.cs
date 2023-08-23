﻿
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
    public class MongoMembershipTableTests_MultipleDeprecated : MembershipTableTestsBase
    {
        public MongoMembershipTableTests_MultipleDeprecated(ConnectionStringFixture fixture, TestEnvironmentFixture environment)
            : base(fixture, environment, new LoggerFilterOptions())
        {
        }

        protected override IMembershipTable CreateMembershipTable(ILogger logger)
        {
            var options = Options.Create(new MongoDBMembershipTableOptions
            {
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest",
                Strategy = MongoDBMembershipStrategy.MultipleDeprecated
            });

            return new MongoMembershipTable(
                MongoDatabaseFixture.DatabaseFactory,
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
                MongoDatabaseFixture.DatabaseFactory,
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
            await MembershipTable_InsertRow(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadRow_Insert_Read()
        {
            await MembershipTable_ReadRow_Insert_Read(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_Insert_ReadAll()
        {
            await MembershipTable_ReadAll_Insert_ReadAll(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRow()
        {
            await MembershipTable_UpdateRow(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRowInParallel()
        {
            await MembershipTable_UpdateRowInParallel(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateIAmAlive()
        {
            await MembershipTable_UpdateIAmAlive(false);
        }
    }
}