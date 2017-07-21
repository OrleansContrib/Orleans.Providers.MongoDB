using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    using System;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using Newtonsoft.Json;
    using System.IO;
    using System.Text;
    using Orleans.Providers.MongoDB.StorageProviders.Serializing;

    /// <summary>
    /// A MongoDB storage provider.
    /// </summary>
    /// <remarks>
    /// The storage provider should be included in a deployment by adding this line to the Orleans server configuration file:
    /// 
    ///     <Provider Type="Orleans.Providers.MongoDB.StorageProviders.MongoDBStorage" Name="MongoDBStore" Database="db-name" ConnectionString="mongodb://YOURHOSTNAME:27017/" />
    ///                                                                                 or
    ///     <Provider Type="Orleans.Providers.MongoDB.StorageProviders.MongoDBStorage" Name="MongoDBStore" Database="" ConnectionString="mongodb://YOURHOSTNAME:27017/db-name" />
    /// and this line to any grain that uses it:
    /// 
    ///     [StorageProvider(ProviderName = "MongoDBStore")]
    /// 
    /// The name 'MongoDBStore' is an arbitrary choice.
    /// </remarks>
    public class MongoDBStorage : BaseJSONStorageProvider
    {
        /// <summary>
        /// Database connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;
            this.ConnectionString = config.Properties["ConnectionString"];

            if (!config.Properties.ContainsKey("Database") || string.IsNullOrEmpty(config.Properties["Database"]))
            {
                this.Database = MongoUrl.Create(this.ConnectionString).DatabaseName;
            }
            else
            {
                this.Database = config.Properties["Database"];
            }

            if (string.IsNullOrWhiteSpace(this.ConnectionString)) throw new ArgumentException("ConnectionString property not set");
            if (string.IsNullOrWhiteSpace(this.Database)) throw new ArgumentException("Database property not set");
            this.DataManager = this.ReturnDataManager(this.Database, this.ConnectionString);
            return base.Init(name, providerRuntime, config);
        }

        public virtual IJSONStateDataManager ReturnDataManager(string database, string connectionString)
        {
            return new GrainStateMongoDataManager(database, connectionString);
        }
    }
}