using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public class JsonGrainStateSerializer : IGrainStateSerializer
    {
        private readonly JsonSerializer serializer;

        public JsonGrainStateSerializer(ITypeResolver typeResolver, IGrainFactory grainFactory)
            : this(JsonSerializer.Create(OrleansJsonSerializer.GetDefaultSerializerSettings(typeResolver, grainFactory)))
        {
        }

        protected JsonGrainStateSerializer(JsonSerializer serializer)
        {
            this.serializer = serializer;

            // https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/44
            this.serializer.NullValueHandling = NullValueHandling.Include;
            this.serializer.DefaultValueHandling = DefaultValueHandling.Populate;
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
