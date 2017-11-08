using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MongoStorageProvider : BaseJSONStorageProvider
    {
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            var connectionString = config.Properties["ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("ConnectionString property not set");
            }
        
            var useJsonFormat = config.GetBoolProperty("UseJsonFormat", true);

            var database = config.Properties["Database"];

            if (string.IsNullOrEmpty(config.Properties["Database"]))
            {
                database = MongoUrl.Create(connectionString).DatabaseName;
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                throw new ArgumentException("Database property not set");
            }

            DataManager = ReturnDataManager(database, connectionString, UseJsonFormat);

            return base.Init(name, providerRuntime, config);
        }

        public virtual IJSONStateDataManager ReturnDataManager(string database, string connectionString, bool UseJsonFormat)
        {
            return new MongoDataManager(database, connectionString);
        }
    }
}