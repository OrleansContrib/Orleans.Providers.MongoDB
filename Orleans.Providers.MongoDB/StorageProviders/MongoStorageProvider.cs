using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MongoStorageProvider : BaseJSONStorageProvider
    {
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            config.Properties.TryGetValue("ConnectionString", out var connectionString);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("ConnectionString property not set");
            }

            config.Properties.TryGetValue("Database", out var database);

            if (string.IsNullOrEmpty(database))
            {
                database = MongoUrl.Create(connectionString).DatabaseName;
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                throw new ArgumentException("Database property not set");
            }

            DataManager = ReturnDataManager(database, connectionString);

            return base.Init(name, providerRuntime, config);
        }

        public virtual IJSONStateDataManager ReturnDataManager(string database, string connectionString)
        {
            return new MongoDataManager(database, connectionString);
        }
    }
}