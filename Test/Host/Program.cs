using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EphemeralMongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;

namespace Orleans.Providers.MongoDB.Test.Host
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var createShardKey = false;

            using var mongoRunner = MongoRunner.Run();
            var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) => siloBuilder
                    .AddReminders()
                    .UseMongoDBClient(mongoRunner.ConnectionString)
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
                    .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("OrleansTestStream")
                    .AddMongoDBGrainStorage("PubSubStore", options =>
                    {
                        options.DatabaseName = "OrleansTestAppPubSubStore";
                        options.CreateShardKeyForCosmos = createShardKey;

                        options.ConfigureJsonSerializerSettings = settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                        };

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
                    .ConfigureLogging(logging => logging.AddConsole()))
                .Build();

            await host.StartAsync();

            var clientHost = new HostBuilder()
                .UseOrleansClient((ctx, clientBuilder) => clientBuilder
                    .UseMongoDBClient(mongoRunner.ConnectionString)
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = "OrleansTestApp";
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "helloworldcluster";
                        options.ServiceId = "helloworldcluster";
                    }))
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            var client = clientHost.Services.GetRequiredService<IClusterClient>();

            await clientHost.StartAsync();

            await TestBasic(client);

            if (!args.Contains("--skip-reminders"))
            {
                await TestReminders(client);
            }

            if (!args.Contains("--skip-streams"))
            {
                await TestStreams(client);
            }

            await TestState(client);
            await TestStateWithCollections(client);

            Console.ReadKey();

            await host.StopAsync();
        }

        private static async Task TestStreams(IClusterClient client)
        {
            var streamProducer = client.GetGrain<IStreamProducerGrain>(0);
            var streamConsumer1 = client.GetGrain<IStreamConsumerGrain>(0);
            var streamConsumer2 = client.GetGrain<IStreamConsumerGrain>(0);

            _ = streamProducer.ProduceEvents();

            await streamConsumer1.Activate();
            await streamConsumer2.Activate();

            await Task.Delay(1000);

            var consumed1 = await streamConsumer1.GetConsumedItems();
            var consumed2 = await streamConsumer1.GetConsumedItems();

            Console.WriteLine("Consumed Events: {0}/{1}", consumed1, consumed2);
        }

        private static async Task TestBasic(IClusterClient client)
        {
            var helloWorldGrain = client.GetGrain<IHelloWorldGrain>(1);

            await helloWorldGrain.SayHello("World");
        }

        private static async Task TestReminders(IClusterClient client)
        {
            var reminderGrain = client.GetGrain<IReminderGrain>(1);

            await reminderGrain.StartReminder("TestReminder", TimeSpan.FromMinutes(1));
        }

        private static async Task TestStateWithCollections(IClusterClient client)
        {
            var vacationEmployee = client.GetGrain<IEmployeeGrain>(2);
            var vacationEmployeeId = await vacationEmployee.ReturnLevel();

            if (vacationEmployeeId == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    await vacationEmployee.AddVacationLeave();
                }

                for (int i = 0; i < 2; i++)
                {
                    await vacationEmployee.AddSickLeave();
                }

                await vacationEmployee.SetLevel(101);
            }

            // Use ObjectCreationHandling.Replace in JsonSerializerSettings to replace the result during deserialization.
            var leaveCount = await vacationEmployee.ReturnLeaveCount();

            Console.WriteLine($"Total leave count: {leaveCount}");
        }

        private static async Task TestState(IClusterClient client)
        {
            var employee = client.GetGrain<IEmployeeGrain>(1);
            var employeeId = await employee.ReturnLevel();

            if (employeeId == 100)
            {
                await employee.SetLevel(50);
            }
            else
            {
                await employee.SetLevel(100);
            }

            employeeId = await employee.ReturnLevel();

            Console.WriteLine(employeeId);
        }
    }
}