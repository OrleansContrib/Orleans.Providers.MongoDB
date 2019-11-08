// ReSharper disable InheritdocConsiderUsage

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Configures MongoDB Membership.
    /// </summary>
    public sealed class MongoDBMembershipTableOptions : MongoDBOptions
    {
        public MongoDBMembershipStrategy Strategy { get; set; }

        public MongoDBMembershipTableOptions()
        {
        }
    }
}
