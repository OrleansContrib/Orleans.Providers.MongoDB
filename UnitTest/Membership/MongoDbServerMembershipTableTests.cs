//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Orleans.Messaging;
//using Orleans.Providers.MongoDB.Membership;
//using Orleans.Runtime;
//using Orleans.Runtime.Configuration;
//using UnitTests.MembershipTests;
//using Xunit;
//using System.Threading;

//namespace Orleans.Providers.MongoDB.UnitTest.Membership
//{
//    public class MongoDbServerMembershipTableTests : MembershipTableTestsBase
//    {
//        private static long Counter = DateTime.UtcNow.Ticks;

//        public MongoDbServerMembershipTableTests() : 
//            base(new MembershipClusterConfiguration())
//        {
//        }

//        protected override IMembershipTable CreateMembershipTable(Logger logger)
//        {
//            return new MongoMembershipTable();
//        }

//        protected override IGatewayListProvider CreateGatewayListProvider(Logger logger)
//        {
//            return new MongoMembershipTable();
//        }

//        [Fact]
//        public async Task MembershipTable_MongoDB_GetGateways()
//        {
//            await MembershipTable_GetGateways();
//        }

//        [Fact]
//        public async Task MembershipTable_MongoDB_ReadAll_EmptyTable()
//        {
//            await MembershipTable_ReadAll_EmptyTable();
//        }

//        [Fact]
//        public async Task MembershipTable_MongoDB_InsertRow()
//        {
//            await MembershipTable_InsertRow(false);
//        }

//        [Fact]
//        public async Task MembershipTable_MongoDB_ReadRow_Insert_Read()
//        {
//            await MembershipTable_ReadRow_Insert_Read(false);
//        }

//        [Fact]
//        public async Task MembershipTable_MongoDB_ReadAll_Insert_ReadAll()
//        {
//            await MembershipTable_ReadAll_Insert_ReadAll(false);
//        }

//        [Fact]
//        public async Task MembershipTable_MongoDB_UpdateRow()
//        {
//            await MembershipTable_UpdateRow(false);
//        }

//        [Fact]
//        public async Task MembershipTable_MongoDB_UpdateRowInParallel()
//        {
//            await MembershipTable_UpdateRowInParallel(false);
//        }

//        [Serializable]
//        public class MembershipClusterConfiguration : ClusterConfiguration
//        {
//            public MembershipClusterConfiguration()
//            {
//                //var config = ClusterConfiguration.LocalhostPrimarySilo();
//                LoadFromFile(@".\OrleansConfiguration.xml");

//                // Init Mongo Membership
//                Globals.DeploymentId = $"OrleansTest{Interlocked.Increment(ref Counter)}";
//                Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
//                Globals.MembershipTableAssembly = "Orleans.Providers.MongoDB";
//                Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Disabled;

//                var n = new NodeConfiguration
//                {
//                    SiloName = "Primary",
//                    ProxyGatewayEndpoint = Defaults.ProxyGatewayEndpoint,
//                    Port = Defaults.Port
//                };
//                Overrides.Add(new KeyValuePair<string, NodeConfiguration>(n.SiloName, n));
//            }
//        }
//    }
//}