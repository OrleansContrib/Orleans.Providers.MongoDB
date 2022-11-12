using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public class JsonGrainStateSerializer : IGrainStateSerializer
    {
        private readonly JsonSerializer serializer;

        public JsonGrainStateSerializer(IServiceProvider serviceProvider, MongoDBGrainStorageOptions options)
        {
            var jsonSettings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(serviceProvider);

            options?.ConfigureJsonSerializerSettings?.Invoke(jsonSettings);

            if (options?.ConfigureJsonSerializerSettings == null)
            {
                //// https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/44
                //// Always include the default value, so that the deserialization process can overwrite default 
                //// values that are not equal to the system defaults.
                jsonSettings.NullValueHandling = NullValueHandling.Include;
                jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
            }

            serializer = JsonSerializer.CreateDefault(jsonSettings);
        }

        public void Deserialize<T>(IGrainState<T> grainState, JObject entityData)
        {
            var jsonReader = new JTokenReader(entityData);

            serializer.Populate(jsonReader, grainState.State);
        }

        public JObject Serialize<T>(IGrainState<T> grainState)
        {
            return JObject.FromObject(grainState.State, serializer);
        }
    }
}
