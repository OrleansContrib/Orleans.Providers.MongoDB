// ReSharper disable InheritdocConsiderUsage

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Configures MongoDB Gateway List Provider.
    /// </summary>
    public sealed class MongoDBGatewayListProviderOptions : MongoDBOptions
    {
        public MongoDBMembershipStrategy Strategy { get; set; }

        public MongoDBGatewayListProviderOptions()
        {
        }
    }
}
