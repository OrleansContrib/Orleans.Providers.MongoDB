namespace Orleans.Providers.MongoDB.Repository.ConnectionManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using global::MongoDB.Driver;

    /// <summary>
    /// Mongo connection manager. Prevents connections being created everytime.
    /// Only one connection will be created per connectionstring
    /// </summary>
    public static class MongoConnectionManager
    {
        /// <summary>
        /// The instances.
        /// </summary>
        private static volatile List<IMongoClient> instances;

        /// <summary>
        /// The sync root.
        /// </summary>
        private static object syncRoot = new Object();

        /// <summary>
        /// The definitions.
        /// </summary>
        private static volatile List<ConnectionDefinitions> definitions;

        /// <summary>
        /// Prevents a default instance of the <see cref="MongoConnectionManager"/> class from being created.
        /// </summary>
        static MongoConnectionManager()
        {
            lock (syncRoot)
            {
                definitions = new List<ConnectionDefinitions>();
                instances = new List<IMongoClient>();
            }
        }

        /// <summary>
        /// The instance.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string.
        /// </param>
        /// <param name="databaseName">
        /// The database name.
        /// </param>
        /// <returns>
        /// The <see cref="IMongoClient"/>.
        /// </returns>
        public static IMongoClient Instance(string connectionString, string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = MongoUrl.Create(connectionString).DatabaseName;
            }

            if (!definitions.Any(d => d.ConnectionString == connectionString && d.Database == databaseName))
            {
                lock (syncRoot)
                {
                    ConnectionDefinitions definition = new ConnectionDefinitions
                    {
                        Index = definitions.Count,
                        ConnectionString = connectionString,
                        Database = databaseName
                    };
                    definitions.Add(definition);

                    var instance = new MongoClient(definitions[0].ConnectionString);
                    instances.Add(instance);

                    return instance;
                }
            }
            else
            {
                var definition = definitions.FirstOrDefault(d => d.ConnectionString == connectionString && d.Database == databaseName);
                return instances[definition.Index];
            }
        }
    }
}
