using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MongoStorageProvider : BaseJSONStorageProvider
    {
        public const string ConnectionStringProperty = "ConnectionString";
        public const string CollectionPrefixProperty = "CollectionPrefix";
        public const string DatabaseNameProperty = "DatabaseProperty";

        private string prefix;

        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            var mongoConnectionString = config.GetProperty(ConnectionStringProperty, string.Empty);
            var mongoCollectionPrefix = config.GetProperty(CollectionPrefixProperty, string.Empty);
            var mongoDatabaseName = config.GetProperty(DatabaseNameProperty, string.Empty);

            prefix = mongoCollectionPrefix;

            DataManager =
                new MongoDataManager(
                    mongoConnectionString, 
                    mongoDatabaseName);

            return base.Init(name, providerRuntime, config);
        }

        protected override string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return prefix + base.ReturnGrainName(grainType, grainReference);
        }

        public virtual IJSONStateDataManager ReturnDataManager(string database, string connectionString)
        {
            return new MongoDataManager(connectionString, database);
        }
    }
}