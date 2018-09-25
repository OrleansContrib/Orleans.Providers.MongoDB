using System;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    /// Internal use only. Provides a wrapper around a custom factory method.
    /// </summary>
    internal class JsonBsonConverterFactoryMethodWrapper : IJsonBsonConverterFactory
    {

        readonly Func<string, IJsonBsonConverter> _create;

        public JsonBsonConverterFactoryMethodWrapper(Func<string, IJsonBsonConverter> create)
            => _create = create;

        public IJsonBsonConverter Create(string grainType)
            => _create(grainType);
    }
}