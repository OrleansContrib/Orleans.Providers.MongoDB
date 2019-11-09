using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;

namespace Microsoft.Extensions.DependencyInjection
{
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
    }
}
