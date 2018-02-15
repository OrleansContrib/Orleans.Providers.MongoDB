using System;
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
        }

        public void Deserialize(IGrainState grainState, JObject entityData)
        {
            var jsonReader = new JTokenReader(entityData);

            serializer.Populate(jsonReader, grainState.State);
        }

        public JObject Serialize(IGrainState grainState)
        {
            throw new NotImplementedException();
        }
    }
}
