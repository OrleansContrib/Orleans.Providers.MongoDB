using System;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    /// Internal use only. Provides the default json-bson converter.
    /// </summary>
    internal class DefaultJsonBsonConverterFactory : IJsonBsonConverterFactory
    {
        static readonly IJsonBsonConverter _instance = new DefaultJsonBsonConverter();

        public IJsonBsonConverter Create(IServiceProvider services, string grainType)
            => _instance;
    }
}