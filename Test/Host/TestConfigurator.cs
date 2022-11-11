using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.TestingHost;
using System;
using System.Net;

namespace Orleans.Providers.MongoDB.Test.Host
{
  internal class TestConfigurator : ISiloConfigurator
  {
    readonly string connectionString = "mongodb://localhost/OrleansTestApp";
    readonly bool createShardKey = false;
    public void Configure(ISiloBuilder siloBuilder)
    {
      siloBuilder.UseMongoDBClient(connectionString)
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
          .AddBroadcastChannel("OrleansTestStream", options =>
          {
            options.FireAndForgetDelivery = true;
          })
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
          .ConfigureLogging(logging => logging.AddConsole());
    }
  }
}