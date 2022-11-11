using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Membership;
using Orleans.Providers.MongoDB.Reminders;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable CheckNamespace

namespace Orleans.Hosting
{
    /// <summary>
    /// Extension methods for configuration classes specific to OrleansMongoUtils.dll 
    /// </summary>
    public static class MongoDBSiloExtensions
    {
        /// <summary>
        /// Configure silo to use MongoDb with a passed in connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public static ISiloBuilder UseMongoDBClient(this ISiloBuilder builder, string connectionString)
        {
            return builder.ConfigureServices(services => services.AddMongoDBClient(connectionString));
        }

        /// <summary>
        /// Configure ISiloBuilder to use MongoReminderTable.
        /// </summary>
        public static ISiloBuilder UseMongoDBReminders(this ISiloBuilder builder,
            Action<MongoDBRemindersOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBReminders(configurator));
        }


        /// <summary>
        /// Configure ISiloBuilder to use MongoReminderTable
        /// </summary>
        public static ISiloBuilder UseMongoDBReminders(this ISiloBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddMongoDBReminders(configuration));
        }

        /// <summary>
        /// Configure ISiloBuilder to use MongoBasedMembership
        /// </summary>
        public static ISiloBuilder UseMongoDBClustering(this ISiloBuilder builder,
            Action<MongoDBMembershipTableOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBMembershipTable(configurator));
        }

        /// <summary>
        /// Configure ISiloBuilder to use MongoMembershipTable
        /// </summary>
        public static ISiloBuilder UseMongoDBMembershipTable(this ISiloBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddMongoDBMembershipTable(configuration));
        }

        /// <summary>
        /// Configure silo to use MongoReminderTable.
        /// </summary>
        public static IServiceCollection AddMongoDBReminders(this IServiceCollection services,
            Action<MongoDBRemindersOptions> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IReminderTable, MongoReminderTable>();
            services.AddSingleton<IConfigurationValidator, MongoDBOptionsValidator<MongoDBRemindersOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use MongoReminderTable.
        /// </summary>
        public static IServiceCollection AddMongoDBReminders(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDBRemindersOptions>(configuration);
            services.AddSingleton<IReminderTable, MongoReminderTable>();
            services.AddSingleton<IConfigurationValidator, MongoDBOptionsValidator<MongoDBRemindersOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use MongoMembershipTable.
        /// </summary>
        public static IServiceCollection AddMongoDBMembershipTable(this IServiceCollection services,
            Action<MongoDBMembershipTableOptions> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IMembershipTable, MongoMembershipTable>();
            services.AddSingleton<IConfigurationValidator, MongoDBOptionsValidator<MongoDBMembershipTableOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use MongoMembershipTable.
        /// </summary>
        public static IServiceCollection AddMongoDBMembershipTable(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDBMembershipTableOptions>(configuration);
            services.AddSingleton<IMembershipTable, MongoMembershipTable>();
            services.AddSingleton<IConfigurationValidator, MongoDBOptionsValidator<MongoDBMembershipTableOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use MongoDB for grain storage.
        /// </summary>
        public static ISiloBuilder AddMongoDBGrainStorage(this ISiloBuilder builder, string name,
            Action<MongoDBGrainStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddMongoDBGrainStorage(name, configureOptions));
        }


        /// <summary>
        /// Configure silo to use MongoDB as the default grain storage.
        /// </summary>
        public static ISiloBuilder AddMongoDBGrainStorageAsDefault(this ISiloBuilder builder,
            Action<OptionsBuilder<MongoDBGrainStorageOptions>> configureOptions = null)
        {
            return builder.AddMongoDBGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }


        /// <summary>
        /// Configure silo to use MongoDB for grain storage.
        /// </summary>
        public static ISiloBuilder AddMongoDBGrainStorage(this ISiloBuilder builder, string name,
            Action<OptionsBuilder<MongoDBGrainStorageOptions>> configureOptions = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBGrainStorage(name, configureOptions));
        }

        /// <summary>
        /// Configure silo to use MongoDB as the default grain storage.
        /// </summary>
        public static IServiceCollection AddMongoDBGrainStorageAsDefault(this IServiceCollection services,
            Action<MongoDBGrainStorageOptions> configureOptions)
        {
            return services.AddMongoDBGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
        }

        /// <summary>
        /// Configure silo to use MongoDB for grain storage.
        /// </summary>
        public static IServiceCollection AddMongoDBGrainStorage(this IServiceCollection services, string name,
            Action<MongoDBGrainStorageOptions> configureOptions)
        {
            return services.AddMongoDBGrainStorage(name, ob => ob.Configure(configureOptions));
        }

        /// <summary>
        /// Configure silo to use MongoDB as the default grain storage.
        /// </summary>
        public static IServiceCollection AddMongoDBGrainStorageAsDefault(this IServiceCollection services,
            Action<OptionsBuilder<MongoDBGrainStorageOptions>> configureOptions = null)
        {
            return services.AddMongoDBGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Configure silo to use MongoDB for grain storage.
        /// </summary>
        public static IServiceCollection AddMongoDBGrainStorage(this IServiceCollection services, string name,
            Action<OptionsBuilder<MongoDBGrainStorageOptions>> configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<MongoDBGrainStorageOptions>(name));

            services.TryAddSingleton(sp => sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
            services.TryAddSingleton<IGrainStateSerializer>(sp => new JsonGrainStateSerializer(sp.GetService<IOptions<OrleansJsonSerializerOptions>>(), sp.GetService<IOptionsMonitor<MongoDBGrainStorageOptions>>().Get(name)));

            services.ConfigureNamedOptionForLogging<MongoDBGrainStorageOptions>(name);

            services.AddTransient<IConfigurationValidator>(sp => new MongoDBGrainStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>().Get(name), name));
            services.AddSingletonNamedService(name, MongoGrainStorageFactory.Create);
            services.AddSingletonNamedService(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));

            return services;
        }
    }
}