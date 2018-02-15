using System;
using MongoDB.Driver;

// ReSharper disable InvertIf

namespace Orleans.Providers.MongoDB.Configuration
{
    /// <summary>
    /// Options to configure MongoDB for Orleans
    /// </summary>
    public class MongoDBOptions
    {
        /// <summary>
        /// Connection string for MongoDB
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The collection prefix.
        /// </summary>
        public string CollectionPrefix { get; set; }

        internal void Validate(string message)
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
            {
                var originalConnectionString = ConnectionString;

                if (string.IsNullOrWhiteSpace(DatabaseName))
                {
                    try
                    {
                        var mongoUrl = MongoUrl.Create(originalConnectionString);

                        DatabaseName = mongoUrl.DatabaseName;
                    }
                    catch
                    {
                        DatabaseName = null;
                    }
                }

                try
                {
                    var mongoUrl = new MongoUrlBuilder(originalConnectionString) { DatabaseName = null };

                    ConnectionString = mongoUrl.ToString();
                }
                catch
                {
                    ConnectionString = originalConnectionString;
                }
            }

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidOperationException($"{message}: Connection string is not defined.");
            }

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                throw new InvalidOperationException($"{message}: Database name string is not defined.");
            }
        }
    }
}
