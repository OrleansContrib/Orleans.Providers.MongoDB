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
        using (var silo = new SiloHost("primary", ClusterConfiguration.LocalhostPrimarySilo()))
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