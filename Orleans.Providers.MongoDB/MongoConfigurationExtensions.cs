using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Membership;
using Orleans.Providers.MongoDB.Reminders;
using Orleans.Providers.MongoDB.Statistics;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Runtime.Configuration;

// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable CheckNamespace

namespace Orleans
{
    /// <summary>
    /// Extension methods for configuration classes specific to OrleansMongoUtils.dll 
    /// </summary>
    public static class MongoConfigurationExtensions
    {
        /// <summary>
        /// Configure ISiloHostBuilder to use MongoBasedMembership
        /// </summary>
        /// <param name="builder"></param>
        public static ISiloHostBuilder UseMongoMembershipTable(this ISiloHostBuilder builder)
        {
            return builder.ConfigureServices(services => services.UseMongoMembershipTable());
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use MongoMembershipTable
        /// </summary>
        /// <param name="builder"></param>
        public static ISiloHostBuilder UseMongoReminderTable(this ISiloHostBuilder builder)
        {
            return builder.ConfigureServices(services => services.UseMongoReminderTable());
        }

        /// <summary>
        /// Configure client to use MongoGatewayListProvider
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IClientBuilder UseMongoGatewayListProvider(this IClientBuilder builder)
        {
            return builder.ConfigureServices(services => services.UseMongoGatewayListProvider());
        }

        /// <summary>
        /// Configure DI container to use MongoBasedMembership
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection UseMongoMembershipTable(this IServiceCollection services)
        {
            services.AddSingleton<IMembershipTable, MongoMembershipTable>();

            return services;
        }

        /// <summary>
        /// Configure DI container to use MongoReminderTable
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection UseMongoReminderTable(this IServiceCollection services)
        {
            services.AddSingleton<IReminderTable, MongoReminderTable>();

            return services;
        }

        /// <summary>
        /// Configure DI container to use MongoGatewayListProvider
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection UseMongoGatewayListProvider(this IServiceCollection services)
        {
            services.AddSingleton<IGatewayListProvider, MongoGatewayListProvider>();

            return services;
        }

        /// <summary>
        /// Adds a statistics provider of type <see cref="MongoStatisticsPublisher"/>
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        public static void AddMongoStatisticsPublisher(
            this ClusterConfiguration config,
            string providerName = "MongoStorageProvider")
        {
            config.Globals.RegisterStatisticsProvider<MongoStatisticsPublisher>("MongoStatisticsPublisher");
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="connectionString">The azure storage connection string. If none is provided, it will use the same as in the Globals configuration.</param>
        /// <param name="database">The database name where to store the state.</param>
        /// <param name="collectionPrefix">The prefix for all collections.</param>
        /// <param name="useJsonFormat">Whether is stores the content as JSON or as binary in Mongo Table.</param>
        public static void AddMongoDBStorageProvider(
            this ClusterConfiguration config,
            string providerName = "MongoStorageProvider",
            string connectionString = null,
            string database = MongoStorageProvider.DatabaseDefault,
            string collectionPrefix = null,
            bool useJsonFormat = false)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            connectionString = GetConnectionString(connectionString, config);

            var properties = new Dictionary<string, string>
            {
                { MongoStorageProvider.ConnectionStringProperty, connectionString },
                { MongoStorageProvider.DatabaseProperty, database },
                { MongoStorageProvider.CollectionPrefixProperty, collectionPrefix },
                { MongoStorageProvider.UseJsonFormatProperty, useJsonFormat.ToString() },
            };

            config.Globals.RegisterStorageProvider<MongoStorageProvider>(providerName, properties);
        }

        private static string GetConnectionString(string connectionString, ClusterConfiguration config)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            if (!string.IsNullOrWhiteSpace(config.Globals.DataConnectionString))
            {
                return config.Globals.DataConnectionString;
            }

            throw new ArgumentNullException(nameof(connectionString), "Parameter value and fallback value are both null or empty.");
        }
    }
}