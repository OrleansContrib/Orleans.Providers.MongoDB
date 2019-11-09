using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Membership;

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
        /// Configure silo to use MongoDb with a passed in connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public static IClientBuilder UseMongoDBClient(this IClientBuilder builder, string connectionString)
        {
            return builder.ConfigureServices(services => services.AddMongoDBClient(connectionString));
        }

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
    }
}