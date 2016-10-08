namespace Orleans.Providers.MongoDB.UnitTest.Reminders
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Orleans.Providers.MongoDB.UnitTest.Base;
    using Orleans.Runtime.Configuration;

    using UnitTests.TimerTests;

    [TestClass]
    public class ReminderTests_Mongo : ReminderTests_Base
    {
        [Serializable]
        public class RemindersClusterConfiguration : ClusterConfiguration
        {
            public RemindersClusterConfiguration()
            {
                //var config = ClusterConfiguration.LocalhostPrimarySilo();
                this.LoadFromFile(@".\OrleansConfiguration.xml");

                // Init Mongo Membership
                this.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
                this.Globals.MembershipTableAssembly = "Orleans.Providers.MongoDB";
                this.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Custom;
                this.Globals.ReminderTableAssembly = "Orleans.Providers.MongoDB";

                var n = new NodeConfiguration
                            {
                                SiloName = "Primary",
                                ProxyGatewayEndpoint = this.Defaults.ProxyGatewayEndpoint,
                                Port = this.Defaults.Port
                };
                this.Overrides.Add(new KeyValuePair<string, NodeConfiguration>(n.SiloName, n));
            }
        }

        public ReminderTests_Mongo() : base(new RemindersClusterConfiguration())
        {
            List<string> hosts = new List<string>();
            hosts.Add("Primary");

            this.Deploy(hosts);

            var controlProxy = GrainClient.GrainFactory.GetGrain<IReminderTestGrain2>(Guid.NewGuid());
            controlProxy.EraseReminderTable(this.ClusterConfiguration.Globals.DataConnectionString).WaitWithThrow(TestConstants.InitTimeout);
        }

        [TestMethod]
        public async Task Rem_Sql_Basic_StopByRef()
        {
            try
            {
                await Test_Reminders_Basic_StopByRef();
            }
            catch (Exception ex)
            {
                
                throw;
            }
        }

        [TestMethod]
        public async Task Rem_Sql_Basic_ListOps()
        {
            try
            {
                await Test_Reminders_Basic_ListOps();
            }
            catch (Exception ex)
            {
                
                throw;
            }
        }

        //[TestMethod]
        //public async Task Rem_Sql_1J_MultiGrainMultiReminders()
        //{
        //    try
        //    {
        //        await Test_Reminders_1J_MultiGrainMultiReminders();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        [TestMethod]
        public async Task Rem_Sql_ReminderNotFound()
        {
            try
            {
                await Test_Reminders_ReminderNotFound();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
