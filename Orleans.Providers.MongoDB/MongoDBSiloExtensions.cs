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
        /// Configure ISiloHostBuilder to use MongoReminderTable.
        /// </summary>
        public static ISiloHostBuilder UseMongoDBReminders(this ISiloHostBuilder builder,
            Action<MongoDBRemindersOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBReminders(configurator));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use MongoReminderTable
        /// </summary>
        public static ISiloHostBuilder UseMongoDBReminders(this ISiloHostBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddMongoDBReminders(configuration));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use MongoBasedMembership
        /// </summary>
        public static ISiloHostBuilder UseMongoDBClustering(this ISiloHostBuilder builder,
            Action<MongoDBMembershipTableOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBMembershipTable(configurator));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use MongoMembershipTable
        /// </summary>
        public static ISiloHostBuilder UseMongoDBMembershipTable(this ISiloHostBuilder builder,
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
            services.AddSingleton<IConfigurationValidator, MongoDBOptionsValidator<MongoDBRemindersOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use MongoDB as the default grain storage.
        /// </summary>
        public static ISiloHostBuilder AddMongoDBGrainStorageAsDefault(this ISiloHostBuilder builder,
            Action<MongoDBGrainStorageOptions> configureOptions)
        {
            return builder.AddMongoDBGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Configure silo to use MongoDB for grain storage.
        /// </summary>
        public static ISiloHostBuilder AddMongoDBGrainStorage(this ISiloHostBuilder builder, string name,
            Action<MongoDBGrainStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddMongoDBGrainStorage(name, configureOptions));
        }

        /// <summary>
        /// Configure silo to use MongoDB as the default grain storage.
        /// </summary>
        public static ISiloHostBuilder AddMongoDBGrainStorageAsDefault(this ISiloHostBuilder builder,
            Action<OptionsBuilder<MongoDBGrainStorageOptions>> configureOptions = null)
        {
            return builder.AddMongoDBGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Configure silo to use MongoDB for grain storage.
        /// </summary>
        public static ISiloHostBuilder AddMongoDBGrainStorage(this ISiloHostBuilder builder, string name,
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
            services.TryAddSingleton<IGrainStateSerializer, JsonGrainStateSerializer>();
            services.TryAddSingleton<IJsonBsonConverterFactory>(new DefaultJsonBsonConverterFactory());

            services.ConfigureNamedOptionForLogging<MongoDBGrainStorageOptions>(name);

            services.AddTransient<IConfigurationValidator>(sp => new MongoDBGrainStorageOptionsValidator(sp.GetService<IOptionsSnapshot<MongoDBGrainStorageOptions>>().Get(name), name));
            services.AddSingletonNamedService(name, MongoGrainStorageFactory.Create);
            services.AddSingletonNamedService(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));

            return services;
        }

        // ========== IJsonBsonConverterFactory ============

        public static ISiloHostBuilder UseMongoDBDefaultJsonBsonConverterFactory(this ISiloHostBuilder builder)
        {
            return builder.ConfigureServices(services => services.AddMongoDBDefaultJsonBsonConverterFactory());
        }

        public static ISiloHostBuilder UseMongoDBJsonBsonConverterFactory(this ISiloHostBuilder builder, IJsonBsonConverterFactory converterFactory)
        {
            return builder.ConfigureServices(services => services.AddMongoDBJsonBsonConverter(converterFactory));
        }

        public static ISiloHostBuilder UseMongoDBJsonBsonConverterFactory(this ISiloHostBuilder builder, Func<string, IJsonBsonConverter> create)
        {
            return builder.ConfigureServices(services => services.AddMongoDBJsonBsonConverter(create));
        }

        public static IServiceCollection AddMongoDBDefaultJsonBsonConverterFactory(this IServiceCollection services)
        {
            return services.AddSingleton<IJsonBsonConverterFactory>(new DefaultJsonBsonConverterFactory());
        }

        public static IServiceCollection AddMongoDBJsonBsonConverter(this IServiceCollection services, IJsonBsonConverterFactory converterFactory)
        {
            return services.AddSingleton<IJsonBsonConverterFactory>(converterFactory);
        }

        public static IServiceCollection AddMongoDBJsonBsonConverter(this IServiceCollection services, Func<string, IJsonBsonConverter> create)
        {
            return services.AddSingleton<IJsonBsonConverterFactory>(new JsonBsonConverterFactoryMethodWrapper(create));
        }
    }
}