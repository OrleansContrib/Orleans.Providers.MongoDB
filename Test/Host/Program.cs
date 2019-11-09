using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Providers.MongoDB.Test.Grains;

namespace Orleans.Providers.MongoDB.Test.Host
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var connectionString = "mongodb://localhost/OrleansTestApp";
            var createShardKey = false;

            var silo = new SiloHostBuilder()
                .ConfigureApplicationParts(options =>
                {
                    options.AddApplicationPart(typeof(EmployeeGrain).Assembly).WithReferences();
                })
                .UseMongoDBClient(connectionString)
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = "OrleansTestApp";
                    options.CreateShardKeyForCosmos = createShardKey;
                })
                .AddStartupTask(async (s, ct) =>
                {
                    var grainFactory = s.GetRequiredService<IGrainFactory>();

                    await grainFactory.GetGrain<IHelloWorldGrain>((int)DateTime.UtcNow.TimeOfDay.Ticks).SayHello("HI");
                })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = "OrleansTestApp";
                    options.CreateShardKeyForCosmos = createShardKey;
                })
                .AddMongoDBGrainStorage("MongoDBStore", options =>
                {
                    options.DatabaseName = "OrleansTestApp";
                    options.CreateShardKeyForCosmos = createShardKey;

                    options.ConfigureJsonSerializerSettings = settings =>
                    {
                        settings.NullValueHandling = NullValueHandling.Include;
                        settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                        settings.DefaultValueHandling = DefaultValueHandling.Populate;
                    };

                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "helloworldcluster";
                    options.ServiceId = "helloworldcluster";
                })
                .ConfigureEndpoints(IPAddress.Loopback, 11111, 30000)
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            silo.StartAsync().Wait();

            var client = new ClientBuilder()
                .ConfigureApplicationParts(options =>
                {
                    options.AddApplicationPart(typeof(IHelloWorldGrain).Assembly);
                })
                .UseMongoDBClient(connectionString)
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = "OrleansTestApp";
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "helloworldcluster";
                    options.ServiceId = "helloworldcluster";
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            client.Connect().Wait();

            // get a reference to the grain from the grain factory
            var helloWorldGrain = client.GetGrain<IHelloWorldGrain>(1);

            // call the grain
            helloWorldGrain.SayHello("World").Wait();

            if (!args.Contains("--ship-reminders"))
            {
                var reminderGrain = client.GetGrain<INewsReminderGrain>(1);

                reminderGrain.StartReminder("TestReminder", TimeSpan.FromMinutes(1)).Wait();
            }

            // Test State 
            var employee = client.GetGrain<IEmployeeGrain>(1);
            var employeeId = employee.ReturnLevel().Result;

            if (employeeId == 100)
            {
                employee.SetLevel(50);
            }
            else
            {
                employee.SetLevel(100);
            }

            employeeId = employee.ReturnLevel().Result;

            Console.WriteLine(employeeId);

            // Test collections
            var vacationEmployee = client.GetGrain<IEmployeeGrain>(2);
            var vacationEmployeeId = vacationEmployee.ReturnLevel().Result;

            if (vacationEmployeeId == 0)
            {                
                for (int i = 0; i < 2; i++)
                {
                    vacationEmployee.AddVacationLeave();
                }

                for (int i = 0; i < 2; i++)
                {
                    vacationEmployee.AddSickLeave();
                }

                vacationEmployee.SetLevel(101);
            }

            // Use ObjectCreationHandling.Replace in JsonSerializerSettings to replace the result during deserialization.
            var leaveCount = vacationEmployee.ReturnLeaveCount().Result;

            Console.WriteLine($"Total leave count: {leaveCount}");

            Console.ReadKey();
            silo.StopAsync().Wait();
        }
    }
}