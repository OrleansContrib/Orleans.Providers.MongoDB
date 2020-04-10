using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Storage;
using System;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public static class MongoGrainStorageFactory
    {
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            var optionsSnapshot = services.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>();

            return ActivatorUtilities.CreateInstance<MongoGrainStorage>(services, optionsSnapshot.Get(name));
        }
    }
}
