using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public class JsonGrainStateSerializer : IGrainStateSerializer
    {
        private readonly JsonSerializer serializer;

        public JsonGrainStateSerializer(ITypeResolver typeResolver, IGrainFactory grainFactory, MongoDBGrainStorageOptions options)
        {
            var jsonSettings = OrleansJsonSerializer.GetDefaultSerializerSettings(typeResolver, grainFactory);           

            options?.ConfigureJsonSerializerSettings?.Invoke(jsonSettings);
            this.serializer = JsonSerializer.Create(jsonSettings);

            if (options?.ConfigureJsonSerializerSettings == null)
            {
                //// https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/44
                //// Always include the default value, so that the deserialization process can overwrite default 
                //// values that are not equal to the system defaults.
                this.serializer.NullValueHandling = NullValueHandling.Include;
                this.serializer.DefaultValueHandling = DefaultValueHandling.Populate;
            }            
        }

        public void Deserialize(IGrainState grainState, JObject entityData)
        {
            var jsonReader = new JTokenReader(entityData);

            serializer.Populate(jsonReader, grainState.State);
        }

        public JObject Serialize(IGrainState grainState)
        {
            return JObject.FromObject(grainState.State, serializer);
        }
    }
}
