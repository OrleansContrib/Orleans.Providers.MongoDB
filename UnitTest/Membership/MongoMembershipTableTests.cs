﻿using Orleans.Messaging;
using Orleans.Runtime;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Membership;
using Orleans.Providers.MongoDB.Configuration;
using TestExtensions;
using UnitTests;
using UnitTests.MembershipTests;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    [TestCategory("Membership")]
    [TestCategory("Mongo")]
    public class MongoMembershipTableTests : MembershipTableTestsBase
    {
        public MongoMembershipTableTests(ConnectionStringFixture fixture, TestEnvironmentFixture environment)
            : base(fixture, environment, new LoggerFilterOptions())
        {
        }

        protected override IMembershipTable CreateMembershipTable(ILogger logger)
        {
            var options = Options.Create(new MongoDBMembershipTableOptions
            {
                ConnectionString = "mongodb://localhost/OrleansTest",
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest"
            });

            return new MongoMembershipTable(
                loggerFactory.CreateLogger<MongoMembershipTable>(),
                clusterOptions,
                options);
        }

        protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
        {
            var options = Options.Create(new MongoDBGatewayListProviderOptions
            {
                ConnectionString = "mongodb://localhost/OrleansTest",
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest"
            });

            return new MongoGatewayListProvider(
                loggerFactory.CreateLogger<MongoGatewayListProvider>(),
                clusterOptions,
                gatewayOptions,
                options);
        }

        protected override Task<string> GetConnectionString()
        {
            return Task.FromResult("mongodb://localhost/OrleansTest");
        }

        [Fact, TestCategory("Functional")]
        public async Task MembershipTable_MongoDB_GetGateways()
        {
            await MembershipTable_GetGateways();
        }

        [Fact, TestCategory("Functional")]
        public async Task MembershipTable_MongoDB_ReadAll_EmptyTable()
        {
            await MembershipTable_ReadAll_EmptyTable();
        }

        [Fact, TestCategory("Functional")]
        public async Task MembershipTable_MongoDB_InsertRow()
        {
            await MembershipTable_InsertRow(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task MembershipTable_MongoDB_ReadRow_Insert_Read()
        {
            await MembershipTable_ReadRow_Insert_Read(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task MembershipTable_MongoDB_ReadAll_Insert_ReadAll()
        {
            await MembershipTable_ReadAll_Insert_ReadAll(false);
        }

        [Fact, TestCategory("Functional")]
        public async Task MembershipTable_MongoDB_UpdateRow()
        {
            await MembershipTable_UpdateRow(false);
        }

        [Fact]
        public async Task MembershipTable_MongoDB_UpdateRowInParallel()
        {
            await MembershipTable_UpdateRowInParallel(false);
        }

        [Fact]
        public async Task MembershipTable_MongoDB_UpdateIAmAlive()
        {
            await MembershipTable_UpdateIAmAlive(false);
        }
    }
}