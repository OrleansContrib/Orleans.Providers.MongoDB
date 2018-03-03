using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Messaging;
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

namespace Orleans
{
    /// <summary>
    /// Extension methods for configuration classes specific to OrleansMongoUtils.dll 
    /// </summary>
    public static class MongoDBClientExtensions
    {
        /// <summary>
        /// Configure client to use MongoGatewayListProvider
        /// </summary>
        public static IClientBuilder UseMongoDBClustering(this IClientBuilder builder,
            Action<MongoDBGatewayListProviderOptions> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddMongoDBGatewayListProvider(configurator));
        }

        /// <summary>
        /// Configure client to use MongoGatewayListProvider
        /// </summary>
        public static IClientBuilder UseMongoDBClustering(this IClientBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddMongoDBGatewayListProvider(configuration));
        }

        /// <summary>
        /// Configure silo to use MongoGatewayListProvider.
        /// </summary
        public static IServiceCollection AddMongoDBGatewayListProvider(this IServiceCollection services,
            Action<MongoDBGatewayListProviderOptions> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IGatewayListProvider, MongoGatewayListProvider>();
            services.AddSingleton<IConfigurationValidator, MongoDBOptionsValidator<MongoDBGatewayListProviderOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use MongoGatewayListProvider.
        /// </summary>
        public static IServiceCollection AddMongoDBGatewayListProvider(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDBGatewayListProviderOptions>(configuration);
            services.AddSingleton<IGatewayListProvider, MongoGatewayListProvider>();
            services.AddSingleton<IConfigurationValidator, MongoDBOptionsValidator<MongoDBGatewayListProviderOptions>>();

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
        public static ISiloHostBuilder AddMongoDBGrainStorageDefault(this ISiloHostBuilder builder, 
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

            services.ConfigureNamedOptionForLogging<MongoDBGrainStorageOptions>(name);

            services.AddTransient<IConfigurationValidator>(sp => new MongoDBGrainStorageOptionsValidator(sp.GetService<IOptionsSnapshot<MongoDBGrainStorageOptions>>().Get(name), name));
            services.AddSingletonNamedService(name, MongoGrainStorageFactory.Create);
            services.AddSingletonNamedService(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));

            return services;
        }
    }
}