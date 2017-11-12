using System;

// ReSharper disable InheritdocConsiderUsage

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Option to configure MongoDB Storage.
    /// </summary>
    public sealed class MongoDBStatisticsOptions : MongoDBOptions
    {
        /// <summary>
        /// Indicates after which time statistics will expire. Set to null snapshots only.
        /// </summary>
        public TimeSpan? ExpireAfter { get; set; }
    }
}
