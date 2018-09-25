using System;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    /// Creates an IJsonBsonConverter object, allowing you to use different converter implementations for different grain types.
    /// </summary>
    public interface IJsonBsonConverterFactory
    {

        /// <summary>
        /// Creates an IJsonBsonConverter object, allowing you to use different converter implementations for different grain types.
        /// </summary>
        /// <param name="grainType">The grain type that this IJsonBsonConverter will be converting grain state for.</param>
        IJsonBsonConverter Create(string grainType);
    }
}