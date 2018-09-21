using System;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    /// Internal use only. Provides a wrapper around a custom factory method.
    /// </summary>
    internal class JsonBsonConverterFactoryMethodWrapper : IJsonBsonConverterFactory
    {

        readonly Func<IServiceProvider, string, IJsonBsonConverter> _create;

        public JsonBsonConverterFactoryMethodWrapper(Func<IServiceProvider, string, IJsonBsonConverter> create)
            => _create = create;

        public IJsonBsonConverter Create(IServiceProvider services, string grainType)
            => _create(services, grainType);
    }
}