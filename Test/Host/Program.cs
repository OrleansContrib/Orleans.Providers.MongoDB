using System;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Test.Grains;
using Orleans.Runtime.Configuration;

namespace Orleans.Providers.MongoDB.Test.Host
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = ClusterConfiguration.LocalhostPrimarySilo(33333);

            config.Globals.DeploymentId = "OrleansWithMongoDB";
            config.Globals.DataConnectionString = "mongodb://localhost/OrleansTestApp";
            config.AddMongoDBStorageProvider("MongoDBStore", "mongodb://localhost", "OrleansTestApp");

            var silo = new SiloHostBuilder()
                .UseConfiguration(config)
                .AddApplicationPartsFromReferences(typeof(EmployeeGrain).Assembly)
                .UseMongoMembershipTable()
                .UseMongoReminderTable()
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            silo.StartAsync().Wait();

            Console.WriteLine("Silo running. Press key to exit...");
            Console.ReadKey();

            silo.StopAsync().Wait();
        }
    }
}