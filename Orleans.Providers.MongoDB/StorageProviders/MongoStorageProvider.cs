using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MongoStorageProvider : BaseJSONStorageProvider
    {
        public const string ConnectionStringProperty = "ConnectionString";
        public const string DatabaseProperty = "DatabaseProperty";
        public const string DatabaseDefault = "Orleans";
        public const string CollectionPrefixProperty = "CollectionPrefix";

        private string prefix;

        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            config.Properties.TryGetValue(CollectionPrefixProperty, out prefix);
            config.Properties.TryGetValue(ConnectionStringProperty, out var connectionString);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"{ConnectionStringProperty} property not set");
            }

            config.Properties.TryGetValue(DatabaseProperty, out var database);

            if (string.IsNullOrEmpty(database))
            {
                database = MongoUrl.Create(connectionString).DatabaseName;
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                database = DatabaseDefault;
            }

            DataManager = ReturnDataManager(database, connectionString);

            return base.Init(name, providerRuntime, config);
        }

        public override string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return prefix + base.ReturnGrainName(grainType, grainReference);
        }

        public virtual IJSONStateDataManager ReturnDataManager(string database, string connectionString)
        {
            return new MongoDataManager(database, connectionString);
        }
    }
}