using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using System;
// ReSharper disable InconsistentNaming

namespace Microsoft.Extensions.DependencyInjection
{
  // "by-ref" Action<T> delegate
  public delegate void ActionRef<T>(ref T item);

  public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configure silo to use MongoDb with a passed in connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public static IServiceCollection AddMongoDBClient(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IMongoClient>(c => new MongoClient(connectionString));
            services.AddSingleton<IMongoClientFactory, DefaultMongoClientFactory>();

            return services;
        }

        /// <summary>
        /// Configure silo to use MongoDb with a passed in connection string.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configureClientSettings">The configuration delegate to configure the <see cref="MongoClientSettings"/>.</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDBClient(this IServiceCollection services, ActionRef<MongoClientSettings> configureClientSettings)
        {
            if (configureClientSettings == null) 
              throw new ArgumentNullException(nameof(configureClientSettings));
            
            var settings = new MongoClientSettings();
            configureClientSettings(ref settings);

            services.AddSingleton<IMongoClient>(c => new MongoClient(settings));
            services.AddSingleton<IMongoClientFactory, DefaultMongoClientFactory>();

            return services;
        }
    }
}
