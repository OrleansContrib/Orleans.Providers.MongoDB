using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Membership;
using Orleans.Providers.MongoDB.Reminders;
using Orleans.Providers.MongoDB.Statistics;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.V2;
using Orleans.Runtime.Configuration;

// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable CheckNamespace

namespace Orleans
{
    /// <summary>
    /// Extension methods for configuration classes specific to OrleansMongoUtils.dll 
    /// </summary>
    public static class MongoDBConfigurationExtensions
    {
        /// <summary>
        /// Configure ISiloHostBuilder to use MongoReminderTable.
        /// </summary>
        /// <param name="builder">The host builder.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// The silo host builder.
        /// </returns>
        public static ISiloHostBuilder UseMongoDBReminders(this ISiloHostBuilder builder,
            Action<MongoDBRemindersOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBReminders(configurator));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use MongoReminderTable
        /// </summary>
        /// <param name="builder">The host builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>
        /// The silo host builder.
        /// </returns>
        public static ISiloHostBuilder UseMongoDBReminders(this ISiloHostBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddMongoDBReminders(configuration));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use MongoBasedMembership
        /// </summary>
        /// <param name="builder">The host builder.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// The silo host builder.
        /// </returns>
        public static ISiloHostBuilder UseMongoDBMembershipTable(this ISiloHostBuilder builder,
            Action<MongoDBMembershipTableOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBMembershipTable(configurator));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use MongoMembershipTable
        /// </summary>
        /// <param name="builder">The host builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>
        /// The silo host builder.
        /// </returns>
        public static ISiloHostBuilder UseMongoDBMembershipTable(this ISiloHostBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddMongoDBReminders(configuration));
        }

        /// <summary>
        /// Configure client to use MongoGatewayListProvider
        /// </summary>
        /// <param name="builder">The client builder.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// The client builder.
        /// </returns>
        public static IClientBuilder UseMongoDBGatewayListProvider(this IClientBuilder builder,
            Action<MongoDBGatewayListProviderOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBGatewayListProvider(configurator));
        }

        /// <summary>
        /// Configure client to use MongoGatewayListProvider
        /// </summary>
        /// <param name="builder">The client builder.</param>
        /// <param name="configuration">The configurator.</param>
        /// <returns>
        /// The client builder.
        /// </returns>
        public static IClientBuilder UseMongoDBGatewayListProvider(this IClientBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddMongoDBGatewayListProvider(configuration));
        }

        /// <summary>
        /// Configure DI container to use MongoReminderTable.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// The service collection
        /// </returns>
        public static IServiceCollection AddMongoDBReminders(this IServiceCollection services,
            Action<MongoDBRemindersOptions> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IReminderTable, MongoReminderTable>();

            return services;
        }

        /// <summary>
        /// Configure DI container to use MongoReminderTable.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configurator.</param>
        /// <returns>
        /// The service collection
        /// </returns>
        public static IServiceCollection AddMongoDBReminders(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDBRemindersOptions>(configuration);
            services.AddSingleton<IReminderTable, MongoReminderTable>();

            return services;
        }

        /// <summary>
        /// Configure DI container to use MongoMembershipTable.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// The service collection
        /// </returns>
        public static IServiceCollection AddMongoDBMembershipTable(this IServiceCollection services,
            Action<MongoDBMembershipTableOptions> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IMembershipTable, MongoMembershipTable>();

            return services;
        }

        /// <summary>
        /// Configure DI container to use MongoMembershipTable.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configurator.</param>
        /// <returns>
        /// The service collection
        /// </returns>
        public static IServiceCollection AddMongoDBMembershipTable(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDBMembershipTableOptions>(configuration);
            services.AddSingleton<IMembershipTable, MongoMembershipTable>();

            return services;
        }

        /// <summary>
        /// Configure DI container to use MongoGatewayListProvider.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// The service collection
        /// </returns>
        public static IServiceCollection AddMongoDBGatewayListProvider(this IServiceCollection services,
            Action<MongoDBGatewayListProviderOptions> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IGatewayListProvider, MongoGatewayListProvider>();

            return services;
        }

        /// <summary>
        /// Configure DI container to use MongoGatewayListProvider.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>
        /// The service collection
        /// </returns>
        public static IServiceCollection AddMongoDBGatewayListProvider(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDBGatewayListProviderOptions>(configuration);
            services.AddSingleton<IGatewayListProvider, MongoGatewayListProvider>();

            return services;
        }
        
        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configurator">The configurator.</param>
        public static void AddMongoDBStorageProvider(this ClusterConfiguration config, string providerName,
            Action<MongoDBStorageOptions> configurator = null)
        {
            var options = new MongoDBStorageOptions();

            configurator?.Invoke(options);

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStorageProvider<MongoStorageProvider>(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddMongoDBStorageProvider(this ClusterConfiguration config, string providerName,
            IConfiguration configuration)
        {
            var options = configuration.Get<MongoDBStorageOptions>();

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStorageProvider<MongoStorageProvider>(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configurator">The configurator.</param>
        public static void AddMongoDBStorageProvider<T>(this ClusterConfiguration config, string providerName,
            Action<MongoDBStorageOptions> configurator = null) where T : MongoStorageProvider
        {
            var options = new MongoDBStorageOptions();

            configurator?.Invoke(options);

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStorageProvider<T>(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configurator">The configurator.</param>
        public static void AddMongoDBStorageProviderV2(this ClusterConfiguration config, string providerName,
            Action<MongoDBStorageOptions> configurator = null)
        {
            var options = new MongoDBStorageOptions();

            configurator?.Invoke(options);

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStorageProviderV2<MongoStorageProviderV2>(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddMongoDBStorageProviderV2(this ClusterConfiguration config, string providerName,
            IConfiguration configuration)
        {
            var options = configuration.Get<MongoDBStorageOptions>();

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStorageProviderV2<MongoStorageProviderV2>(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configurator">The configurator.</param>
        public static void AddMongoDBStorageProviderV2<T>(this ClusterConfiguration config, string providerName,
            Action<MongoDBStorageOptions> configurator = null) where T : MongoStorageProviderV2
        {
            var options = new MongoDBStorageOptions();

            configurator?.Invoke(options);

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStorageProviderV2<T>(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStorageProvider"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddMongoDBStorageProviderV2<T>(this ClusterConfiguration config, string providerName,
            IConfiguration configuration) where T : MongoStorageProviderV2
        {
            var options = configuration.Get<MongoDBStorageOptions>();

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStorageProviderV2<T>(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStatisticsPublisher"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configurator">The configurator.</param>
        public static void AddMongoDBStatisticsProvider(this ClusterConfiguration config, string providerName,
            Action<MongoDBStatisticsOptions> configurator = null)
        {
            var options = new MongoDBStatisticsOptions();

            configurator?.Invoke(options);

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStatisticsProvider(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStatisticsPublisher"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddMongoDBStatisticsProvider(this ClusterConfiguration config, string providerName,
            IConfiguration configuration)
        {
            var options = configuration.Get<MongoDBStatisticsOptions>();

            options.EnrichAndValidate(config.Globals, false);

            AddMongoDBStatisticsProvider(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStatisticsPublisher"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configurator">The configurator.</param>
        public static void AddMongoDBStatisticsProvider(this ClientConfiguration config, string providerName,
            Action<MongoDBStatisticsOptions> configurator = null)
        {
            var options = new MongoDBStatisticsOptions();

            configurator?.Invoke(options);

            options.EnrichAndValidate(config);

            AddMongoDBStatisticsProvider(config, providerName, options);
        }

        /// <summary>
        /// Adds a storage provider of type <see cref="MongoStatisticsPublisher"/>.
        /// </summary>
        /// <param name="config">The cluster configuration object to add provider to.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddMongoDBStatisticsProvider(this ClientConfiguration config, string providerName,
            IConfiguration configuration)
        {
            var options = configuration.Get<MongoDBStatisticsOptions>();

            options.EnrichAndValidate(config);

            AddMongoDBStatisticsProvider(config, providerName, options);
        }

        private static void AddMongoDBStorageProvider<T>(ClusterConfiguration config, string providerName, MongoDBStorageOptions options) where T : MongoStorageProvider
        {
            var properties = new Dictionary<string, string>
            {
                { MongoStorageProvider.ConnectionStringProperty, options.ConnectionString },
                { MongoStorageProvider.CollectionPrefixProperty, options.CollectionPrefix },
                { MongoStorageProvider.DatabaseNameProperty, options.DatabaseName },
                { MongoStorageProvider.UseJsonFormatProperty, options.UseJsonFormat.ToString() }
            };

            config.Globals.RegisterStorageProvider<T>(providerName, properties);
        }

        private static void AddMongoDBStorageProviderV2<T>(ClusterConfiguration config, string providerName, MongoDBStorageOptions options) where T : MongoStorageProviderV2
        {
            var properties = new Dictionary<string, string>
            {
                { MongoStorageProviderV2.ConnectionStringProperty, options.ConnectionString },
                { MongoStorageProviderV2.CollectionPrefixProperty, options.CollectionPrefix },
                { MongoStorageProviderV2.DatabaseNameProperty, options.DatabaseName }
            };

            config.Globals.RegisterStorageProvider<T>(providerName, properties);
        }

        private static void AddMongoDBStatisticsProvider(ClusterConfiguration config, string providerName, MongoDBStatisticsOptions options)
        {
            var properties = new Dictionary<string, string>
            {
                { MongoStatisticsPublisher.ConnectionStringProperty, options.ConnectionString },
                { MongoStatisticsPublisher.CollectionPrefixProperty, options.CollectionPrefix },
                { MongoStatisticsPublisher.DatabaseNameProperty, options.DatabaseName },
                { MongoStatisticsPublisher.ExpireAfterProperty, options.ExpireAfter.ToString() }
            };

            config.Globals.RegisterStatisticsProvider<MongoStatisticsPublisher>(providerName, properties);
            config.Defaults.StatisticsProviderName = providerName;
        }

        private static void AddMongoDBStatisticsProvider(ClientConfiguration config, string providerName, MongoDBStatisticsOptions options)
        {
            var properties = new Dictionary<string, string>
            {
                { MongoStatisticsPublisher.ConnectionStringProperty, options.ConnectionString },
                { MongoStatisticsPublisher.CollectionPrefixProperty, options.CollectionPrefix },
                { MongoStatisticsPublisher.DatabaseNameProperty, options.DatabaseName },
                { MongoStatisticsPublisher.ExpireAfterProperty, options.ExpireAfter.ToString() }
            };

            config.RegisterStatisticsProvider<MongoStatisticsPublisher>(providerName, properties);
            config.StatisticsProviderName = providerName;
        }
    }
}