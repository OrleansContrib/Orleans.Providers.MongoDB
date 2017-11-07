using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    ///     A MongoDB storage provider.
    /// </summary>
    /// <remarks>
    ///     The storage provider should be included in a deployment by adding this line to the Orleans server configuration
    ///     file:
    ///     <Provider Type="Orleans.Providers.MongoDB.StorageProviders.MongoDBStorage" Name="MongoDBStore" Database="db-name"
    ///         ConnectionString="mongodb://YOURHOSTNAME:27017/" />
    ///     or
    ///     <Provider Type="Orleans.Providers.MongoDB.StorageProviders.MongoDBStorage" Name="MongoDBStore" Database=""
    ///         ConnectionString="mongodb://YOURHOSTNAME:27017/db-name" />
    ///     and this line to any grain that uses it:
    ///     [StorageProvider(ProviderName = "MongoDBStore")]
    ///     The name 'MongoDBStore' is an arbitrary choice.
    /// </remarks>
    public class MongoDBStorage : BaseJSONStorageProvider
    {
        /// <summary>
        ///     Database connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        ///     Initializes the storage provider.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;
            ConnectionString = config.Properties["ConnectionString"];
            var useJsonFormat = config.GetBoolProperty("UseJsonFormat", true);

            if (!config.Properties.ContainsKey("Database") || string.IsNullOrEmpty(config.Properties["Database"]))
                Database = MongoUrl.Create(ConnectionString).DatabaseName;
            else
                Database = config.Properties["Database"];

            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new ArgumentException("ConnectionString property not set");
            if (string.IsNullOrWhiteSpace(Database)) throw new ArgumentException("Database property not set");
            DataManager = ReturnDataManager(Database, ConnectionString, UseJsonFormat);
            return base.Init(name, providerRuntime, config);
        }

        public virtual IJSONStateDataManager ReturnDataManager(string database, string connectionString, bool UseJsonFormat)
        {
            return new GrainStateMongoDataManager(database, connectionString);
        }
    }
}