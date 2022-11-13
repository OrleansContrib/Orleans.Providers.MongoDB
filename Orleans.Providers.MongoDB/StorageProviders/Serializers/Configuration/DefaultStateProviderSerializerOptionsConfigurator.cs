using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers.Configuration
{
    public class DefaultStateProviderSerializerOptionsConfigurator<TOptions> : IPostConfigureOptions<TOptions>
        where TOptions : class, IStateProviderSerializerOptions
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultStateProviderSerializerOptionsConfigurator{TOptions}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public DefaultStateProviderSerializerOptionsConfigurator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public void PostConfigure(string name, TOptions options)
        {
            if (options.GrainStateSerializer == default)
            {
                // First, try to get a IGrainStateSerializer that was registered with the same name as the State provider
                // If none is found, fallback to system wide default
                options.GrainStateSerializer = _serviceProvider.GetServiceByName<IGrainStateSerializer>(name) ?? _serviceProvider.GetRequiredService<IGrainStateSerializer>();
            }
        }
    }
}
