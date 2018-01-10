using System;

namespace Orleans.Providers.MongoDB.StorageProviders.V2
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BsonJsonAttribute : Attribute
    {
    }
}
