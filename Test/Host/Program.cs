#region Using

using System;

using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;

#endregion

internal class Program
{
    private static void Main(string[] args)
    {
        // Todo: This configuration should not be called from the config file 

        try
        {
            var config = ClusterConfiguration.LocalhostPrimarySilo();
            config.LoadFromFile(@".\OrleansConfiguration.xml");

            using (var silo = new SiloHost("primary", config))
            {
                // Init Mongo Membership
                silo.Config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
                silo.Config.Globals.MembershipTableAssembly = "Orleans.Providers.MongoDB";

                //Enable Reminder Service
                silo.Config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Custom;
                silo.Config.Globals.ReminderTableAssembly = "Orleans.Providers.MongoDB";
                silo.Config.Defaults.StatisticsProviderName = "MongoStatisticsPublisher";

                //silo.Config.Defaults = "Orleans.Providers.MongoDB.Statistics.Repository.MongoStatisticsPublisherRepository";
                silo.InitializeOrleansSilo();

                var result = silo.StartOrleansSilo();
                if (result)
                {
                    Console.WriteLine("silo running");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("could not start silo");
                Console.ReadKey();
            }
        }
        catch (Exception ex)
        {
            string a = "";
            throw;
        }
    }
}