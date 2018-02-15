using System;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Test.Grains;

namespace Orleans.Providers.MongoDB.Test.Host
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var silo = new SiloHostBuilder()
                .ConfigureApplicationParts(options =>
                {
                    options.AddApplicationPart(typeof(EmployeeGrain).Assembly);
                })
                .UseMongoDBMembershipTable(options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .UseMongoDBReminders(options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .AddMongoDBGrainStorageAsDefault(options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            silo.StartAsync().Wait();

            Console.WriteLine("Silo running. Press key to exit...");
            Console.ReadKey();

            silo.StopAsync().Wait();
        }
    }
}