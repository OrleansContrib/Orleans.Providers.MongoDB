using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using System;

// ReSharper disable InconsistentNaming

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configure silo to use MongoDb with a passed in connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDBClient(this IServiceCollection services, string connectionString)
        {
            return services.AddMongoDBClient(provider => MongoClientSettings.FromConnectionString(connectionString));
        }

        /// <summary>
        /// Configure silo to use MongoDb with a passed in connection string.
        /// </summary>
        /// <param name="settingsFactory">The settings factory.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">settingsFactory</exception>
        public static IServiceCollection AddMongoDBClient(this IServiceCollection services, Func<IServiceProvider, MongoClientSettings> settingsFactory)
        {
            if (settingsFactory == null)
                throw new ArgumentNullException(nameof(settingsFactory));

            services.TryAddSingleton<IMongoClient>(provider =>
            {
                var settings = settingsFactory(provider);
                return new MongoClient(settings);
            });

            services.TryAddSingleton<IMongoClientFactory, DefaultMongoClientFactory>();

            return services;
        }
    }
}
