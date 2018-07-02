// ReSharper disable InheritdocConsiderUsage

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Option to configure MongoDB Storage.
    /// </summary>
    public class MongoDBGrainStorageOptions : MongoDBOptions
    {
        public bool DateTimeStoredAsString { get; set; } = true;
        public bool DateTimeDeserializedAsLocal { get; set; } = false;

        public MongoDBGrainStorageOptions()
        {
            CollectionPrefix = "Grains";
        }
    }
}
