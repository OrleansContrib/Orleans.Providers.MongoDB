using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.Repository.ConnectionManager
{
    /// <summary>
    ///     Mongo connection manager. Prevents connections being created everytime.
    ///     Only one connection will be created per connectionstring
    /// </summary>
    public static class MongoConnectionManager
    {
        /// <summary>
        ///     The instances.
        /// </summary>
        private static volatile List<IMongoClient> instances;

        /// <summary>
        ///     The sync root.
        /// </summary>
        private static readonly object syncRoot = new object();

        /// <summary>
        ///     The definitions.
        /// </summary>
        private static volatile List<ConnectionDefinitions> definitions;

        /// <summary>
        ///     Prevents a default instance of the <see cref="MongoConnectionManager" /> class from being created.
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
        ///     The instance.
        /// </summary>
        /// <param name="connectionString">
        ///     The connection string.
        /// </param>
        /// <param name="databaseName">
        ///     The database name.
        /// </param>
        /// <returns>
        ///     The <see cref="IMongoClient" />.
        /// </returns>
        public static IMongoClient Instance(string connectionString, string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                databaseName = MongoUrl.Create(connectionString).DatabaseName;

            if (!definitions.Any(d => d.ConnectionString == connectionString && d.Database == databaseName))
                lock (syncRoot)
                {
                    var definition = new ConnectionDefinitions
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
            {
                var definition = definitions.FirstOrDefault(d =>
                    d.ConnectionString == connectionString && d.Database == databaseName);
                return instances[definition.Index];
            }
        }
    }
}