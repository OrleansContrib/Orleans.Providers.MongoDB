using System;
using MongoDB.Driver;
using Orleans.Runtime.Configuration;

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

        public void EnrichAndValidate(ClientConfiguration clientConfiguration)
        {
            if (clientConfiguration != null)
            {
                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    ConnectionString = clientConfiguration.DataConnectionString;
                }
            }

            CollectDatabaseFromConnectionString();
            CleanConnectionString();

            Validate();
        }

        public void EnrichAndValidate(GlobalConfiguration globalConfiguration, bool forReminder)
        {
            if (globalConfiguration != null)
            {
                if (string.IsNullOrWhiteSpace(ConnectionString) && forReminder)
                {
                    ConnectionString = globalConfiguration.DataConnectionStringForReminders;
                }

                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    ConnectionString = globalConfiguration.DataConnectionString;
                }
            }

            CollectDatabaseFromConnectionString();
            CleanConnectionString();

            Validate();
        }

        private void CleanConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
            {
                try
                {
                    var mongoUrl = new MongoUrlBuilder(ConnectionString) { DatabaseName = null };

                    ConnectionString = mongoUrl.ToString();
                }
                catch
                {
                    /* NOOP */
                }
            }
        }

        private void CollectDatabaseFromConnectionString()
        {
            if (string.IsNullOrWhiteSpace(DatabaseName) && !string.IsNullOrWhiteSpace(ConnectionString))
            {
                try
                {
                    var mongoUrl = MongoUrl.Create(ConnectionString);

                    DatabaseName = mongoUrl.DatabaseName;
                }
                catch
                {
                    DatabaseName = null;
                }
            }
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidOperationException("Connection string is not defined.");
            }

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                throw new InvalidOperationException("Database name string is not defined.");
            }
        }
    }
}
