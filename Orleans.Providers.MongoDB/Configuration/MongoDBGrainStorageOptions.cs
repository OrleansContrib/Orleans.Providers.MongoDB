// ReSharper disable InheritdocConsiderUsage

using Newtonsoft.Json;
using System;

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
    }
}
