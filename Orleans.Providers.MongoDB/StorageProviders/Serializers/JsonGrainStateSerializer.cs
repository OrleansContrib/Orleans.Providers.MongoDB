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
        private readonly JsonSerializerSettings _jsonSettings;

        public JsonGrainStateSerializer(IOptions<JsonGrainStateSerializerOptions> options, IServiceProvider serviceProvider)
        {
            _jsonSettings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(serviceProvider);
            options.Value.ConfigureJsonSerializerSettings(_jsonSettings);
        }

        public T Deserialize<T>(BsonValue value)
        {
            using var jsonReader = new JTokenReader(value.ToJToken());
            // Creating a new serializer instance to avoid thread-safety issue: https://github.com/JamesNK/Newtonsoft.Json/issues/1452
            var jsonSerializer = JsonSerializer.CreateDefault(_jsonSettings);
            return jsonSerializer.Deserialize<T>(jsonReader);
        }

        public BsonValue Serialize<T>(T state)
        {
            var jsonSerializer = JsonSerializer.CreateDefault(_jsonSettings);
            return JObject.FromObject(state, jsonSerializer).ToBson();
        }
    }
}
