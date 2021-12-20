// ReSharper disable InheritdocConsiderUsage

using Newtonsoft.Json;
using System;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Option to configure MongoDB Storage.
    /// </summary>
    public class MongoDBGrainStorageOptions : MongoDBOptions
    {
        public MongoDBGrainStorageOptions()
        {
            CollectionPrefix = "Grains";
        }

        public Action<JsonSerializerSettings> ConfigureJsonSerializerSettings { get; set; }

        /// <summary>
        /// The key generation strategy used by the storage provider. It defaults to calling ToKeyString
        /// in the grain reference.
        /// </summary>
        public GrainStorageKeyGenerator KeyGenerator { get; set; } = x => x.ToKeyString();

        internal override void Validate(string name = null)
        {
            base.Validate(name);

            if (KeyGenerator == null)
                throw new OrleansConfigurationException($"{nameof(KeyGenerator)} is required and cannot be null.");

        }
    }

    /// <summary>
    /// Delegate representing a strategy for generating the key of the persisted state in MongoDB.
    /// </summary>
    public delegate string GrainStorageKeyGenerator(GrainReference grainReference);
}
