using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Orleans.Messaging;
    using Orleans.Providers.MongoDB.Membership;
    using Orleans.Providers.MongoDB.Reminders;
    using Orleans.Runtime;
    using Orleans.Runtime.Configuration;

    using UnitTests.MembershipTests;

    [TestClass]
    public class MongoDbServerMembershipTableTests : MembershipTableTestsBase
    {
        [Serializable]
        public class MembershipClusterConfiguration : ClusterConfiguration
        {
            public MembershipClusterConfiguration()
            {
                //var config = ClusterConfiguration.LocalhostPrimarySilo();
                this.LoadFromFile(@".\OrleansConfiguration.xml");

                // Init Mongo Membership
                this.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
                this.Globals.MembershipTableAssembly = "Orleans.Providers.MongoDB";
                this.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Disabled;

                var n = new NodeConfiguration
                {
                    SiloName = "Primary",
                    ProxyGatewayEndpoint = this.Defaults.ProxyGatewayEndpoint,
                    Port = this.Defaults.Port
                };
                this.Overrides.Add(new KeyValuePair<string, NodeConfiguration>(n.SiloName, n));
            }
        }

        public MongoDbServerMembershipTableTests() : base(new MembershipClusterConfiguration())
        {
            string a = "";
            //LogManager.AddTraceLevelOverride(typeof(SqlServerMembershipTableTests).Name, Severity.Verbose3);
        }

        protected override IMembershipTable CreateMembershipTable(Logger logger)
        {
            return new MongoMembershipTable { };
        }

        protected override IGatewayListProvider CreateGatewayListProvider(Logger logger)
        {
            return new MongoMembershipTable();
        }

        //protected override string GetAdoInvariant()
        //{
        //    return AdoNetInvariants.InvariantNameSqlServer;
        //}

        //protected override string GetConnectionString()
        //{
        //    return
        //        RelationalStorageForTesting.SetupInstance(GetAdoInvariant(), testDatabaseName)
        //            .Result.CurrentConnectionString;
        //}

        [TestMethod]
        public void MembershipTable_MongoDB_Init()
        {
        }

        [TestMethod]
        public async Task MembershipTable_MongoDB_GetGateways()
        {
            await MembershipTable_GetGateways();
        }

        [TestMethod]
        public async Task MembershipTable_MongoDB_ReadAll_EmptyTable()
        {
            await MembershipTable_ReadAll_EmptyTable();
        }

        [TestMethod]
        public async Task MembershipTable_MongoDB_InsertRow()
        {
            await MembershipTable_InsertRow();
        }

        [TestMethod]
        public async Task MembershipTable_MongoDB_ReadRow_Insert_Read()
        {
            await MembershipTable_ReadRow_Insert_Read();
        }

        [TestMethod]
        public async Task MembershipTable_MongoDB_ReadAll_Insert_ReadAll()
        {
            await MembershipTable_ReadAll_Insert_ReadAll();
        }

        [TestMethod]
        public async Task MembershipTable_MongoDB_UpdateRow()
        {
            await MembershipTable_UpdateRow();
        }

        [TestMethod]
        public async Task MembershipTable_MongoDB_UpdateRowInParallel()
        {
            await MembershipTable_UpdateRowInParallel();
        }
    }
}
