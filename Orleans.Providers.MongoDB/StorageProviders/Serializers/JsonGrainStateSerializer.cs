using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public class JsonGrainStateSerializer : IGrainStateSerializer
    {
        private readonly JsonSerializerSettings jsonSettings;

        public JsonGrainStateSerializer(ITypeResolver typeResolver, IGrainFactory grainFactory, MongoDBGrainStorageOptions options)
        {
            jsonSettings = OrleansJsonSerializer.GetDefaultSerializerSettings(typeResolver, grainFactory);           

            options?.ConfigureJsonSerializerSettings?.Invoke(jsonSettings);

            if (options?.ConfigureJsonSerializerSettings == null)
            {
                //// https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/44
                //// Always include the default value, so that the deserialization process can overwrite default 
                //// values that are not equal to the system defaults.
                jsonSettings.NullValueHandling = NullValueHandling.Include;
                jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
            }            
        }

        public void Deserialize(IGrainState grainState, JObject entityData)
        {
            var jsonReader = new JTokenReader(entityData);
            var jsonSerializer = JsonSerializer.CreateDefault(jsonSettings);

            jsonSerializer.Populate(jsonReader, grainState.State);
        }

        public JObject Serialize(IGrainState grainState)
        {
            var jsonSerializer = JsonSerializer.CreateDefault(jsonSettings);

            return JObject.FromObject(grainState.State, jsonSerializer);
        }
    }
}
