using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class FileStorageProvider : BaseJSONStorageProvider
    {
        public FileStorageProvider(ILogger<BaseJSONStorageProvider> logger) 
            : base(logger)
        {
        }

        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            var rootDirectory = config.Properties["RootDirectory"];

            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("RootDirectory property not set");
            }

            DataManager = new FileDataManager(rootDirectory);

            return base.Init(name, providerRuntime, config);
        }
    }
}