using MongoDB.Driver;
using Orleans.Runtime;
using System;

// ReSharper disable InvertIf

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Options to configure MongoDB for Orleans
    /// </summary>
    public class MongoDBOptions
    {
        /// <summary>
        /// Database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The mongo client name.
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// The collection prefix.
        /// </summary>
        public string CollectionPrefix { get; set; }

        /// <summary>
        /// True, to create a shard key when using with cosmos db.
        /// </summary>
        public bool CreateShardKeyForCosmos { get; set; }

        /// <summary>
        /// The collection configurator.
        /// </summary>
        public Action<MongoCollectionSettings> CollectionConfigurator { get; set; }

        internal virtual void Validate(string name = null)
        {
            var typeName = GetType().Name;

            if (!string.IsNullOrWhiteSpace(typeName))
            {
                typeName = $"{typeName} for {name}";
            }

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                throw new OrleansConfigurationException($"Invalid {typeName} values for {nameof(DatabaseName)}. {nameof(DatabaseName)} is required.");
            }
        }
    }
}
