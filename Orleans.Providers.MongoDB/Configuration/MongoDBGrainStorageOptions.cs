// ReSharper disable InheritdocConsiderUsage

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
    }
}
