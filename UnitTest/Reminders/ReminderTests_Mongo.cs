using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.Providers.MongoDB.UnitTest.Base;
using Orleans.Runtime.Configuration;
using UnitTests.TimerTests;

namespace Orleans.Providers.MongoDB.UnitTest.Reminders
{
    [TestClass]
    public class ReminderTests_Mongo : ReminderTests_Base
    {
        public ReminderTests_Mongo() : base(new RemindersClusterConfiguration())
        {
            var hosts = new List<string>();
            hosts.Add("Primary");

            Deploy(hosts);
            GrainClient.Initialize(ClientConfiguration.LoadFromFile(@".\ClientConfiguration.xml"));
            var controlProxy = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());
            controlProxy.EraseReminderTable(ClusterConfiguration.Globals.DataConnectionString)
                .WaitWithThrow(TestConstants.InitTimeout);
        }

        [TestMethod]
        public async Task Rem_MongoDB_Basic_StopByRef()
        {
            await Test_Reminders_Basic_StopByRef();
        }

        [TestMethod]
        public async Task Rem_MongoDB_Basic_ListOps()
        {
            await Test_Reminders_Basic_ListOps();
        }

        [TestMethod]
        public async Task Rem_Sql_1J_MultiGrainMultiReminders()
        {
            //Todo: must be run seperately otherwise it errors.
            await Test_Reminders_1J_MultiGrainMultiReminders();
        }

        [TestMethod]
        public async Task Rem_MongoDB_ReminderNotFound()
        {
            await Test_Reminders_ReminderNotFound();
        }

        [Serializable]
        public class RemindersClusterConfiguration : ClusterConfiguration
        {
            public RemindersClusterConfiguration()
            {
                //var config = ClusterConfiguration.LocalhostPrimarySilo();
                LoadFromFile(@".\OrleansConfiguration.xml");
                // Init Mongo Membership
                Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
                Globals.MembershipTableAssembly = "Orleans.Providers.MongoDB";
                Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Custom;
                Globals.ReminderTableAssembly = "Orleans.Providers.MongoDB";

                var n = new NodeConfiguration
                {
                    SiloName = "Primary",
                    ProxyGatewayEndpoint = Defaults.ProxyGatewayEndpoint,
                    Port = Defaults.Port
                };
                Overrides.Add(new KeyValuePair<string, NodeConfiguration>(n.SiloName, n));
            }
        }
    }
}