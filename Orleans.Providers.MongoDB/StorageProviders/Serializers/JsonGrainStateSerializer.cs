using System;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public class JsonGrainStateSerializer : IGrainStateSerializer
    {
        private readonly JsonSerializer serializer;

        public JsonGrainStateSerializer(IOptions<JsonGrainStateSerializerOptions> options, IServiceProvider serviceProvider)
        {
            var jsonSettings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(serviceProvider);
            options.Value.ConfigureJsonSerializerSettings(jsonSettings);
            serializer = JsonSerializer.CreateDefault(jsonSettings);
        }

        public T Deserialize<T>(BsonValue value)
        {
            using var jsonReader = new JTokenReader(value.ToJToken());
            return serializer.Deserialize<T>(jsonReader);
        }

        public BsonValue Serialize<T>(T state)
        {
            return JObject.FromObject(state, serializer).ToBson();
        }
    }
}
