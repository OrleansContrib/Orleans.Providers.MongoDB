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
        var config = ClusterConfiguration.LocalhostPrimarySilo();
        config.LoadFromFile(@".\OrleansConfiguration.xml");

        using (var silo = new SiloHost("primary", config))
        {
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