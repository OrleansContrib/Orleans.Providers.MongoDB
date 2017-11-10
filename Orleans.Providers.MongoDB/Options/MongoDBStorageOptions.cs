// ReSharper disable InheritdocConsiderUsage

namespace Orleans.Providers.MongoDB
{
    /// <summary>
    /// Option to configure MongoDB Storage.
    /// </summary>
    public class MongoDBStorageOptions : MongoDBOptions
    {
        /// <summary>
        /// Determines whether to store the settings in json (instead of binary).
        /// </summary>
        public bool UseJsonFormat { get; set; }
    }
}
