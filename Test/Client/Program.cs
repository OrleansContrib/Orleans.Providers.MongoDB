#region Using

using System;
using System.Threading;

using Orleans;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Runtime.Configuration;

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
        // initialize the grain client, with some retry logic
        var initialized = false;
        while (!initialized)
        {
            try
            {
                GrainClient.Initialize(ClientConfiguration.LocalhostSilo());
                initialized = GrainClient.IsInitialized;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        // get a reference to the grain from the grain factory
        var grain = GrainClient.GrainFactory.GetGrain<IHelloWorldGrain>(1);

        // call the grain
        var response = grain.SayHello("World").Result;

        Console.WriteLine(response);
        Console.ReadKey();
    }

    #endregion
}