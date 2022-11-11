using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public class JsonGrainStateSerializer : IGrainStateSerializer
    {
        private readonly JsonSerializerSettings jsonSettings;
        private readonly JsonSerializer jsonSerializer;

        public JsonGrainStateSerializer(IOptions<OrleansJsonSerializerOptions> jsonSerializerOptions, MongoDBGrainStorageOptions options)
        {
            jsonSettings = jsonSerializerOptions.Value?.JsonSerializerSettings ?? new JsonSerializerSettings();

            options?.ConfigureJsonSerializerSettings?.Invoke(jsonSettings);

            if (options?.ConfigureJsonSerializerSettings == null)
            {
                //// https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/44
                //// Always include the default value, so that the deserialization process can overwrite default 
                //// values that are not equal to the system defaults.
                jsonSettings.NullValueHandling = NullValueHandling.Include;
                jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
            }
            jsonSerializer = JsonSerializer.CreateDefault(jsonSettings);

        }

        public void Deserialize<T>(IGrainState<T> grainState, JObject entityData)
        {
            using var jsonReader = new JTokenReader(entityData);
            jsonSerializer.Populate(jsonReader, grainState.State);
        }

        public JObject Serialize<T>(IGrainState<T> grainState)
        {
            return JObject.FromObject(grainState.State, jsonSerializer);
        }
    }
}
