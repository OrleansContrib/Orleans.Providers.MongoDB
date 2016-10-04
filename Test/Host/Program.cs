#region Using

using System;

using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;

#endregion

/// <summary>
/// The program.
/// </summary>
internal class Program
{
    #region Other Methods

    /// <summary>
    /// The main.
    /// </summary>
    /// <param name="args">
    /// The args.
    /// </param>
    private static void Main(string[] args)
    {
        // Todo: This configuration should not be called from the config file 
        var config = ClusterConfiguration.LocalhostPrimarySilo();
        config.LoadFromFile(@".\OrleansConfiguration.xml");

        using (var silo = new SiloHost("primary", config))
        {
            // Init Mongo Membership
            silo.Config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
            silo.Config.Globals.MembershipTableAssembly = "Orleans.Providers.MongoDB";

            silo.Config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Disabled;

            silo.InitializeOrleansSilo();

            var result = silo.StartOrleansSilo();
            if (result)
            {
                Console.WriteLine("silo running");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("could not start silo");
        }
    }

    #endregion
}