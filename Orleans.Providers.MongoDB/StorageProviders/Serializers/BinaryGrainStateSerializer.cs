using Newtonsoft.Json.Linq;
using Orleans.Serialization;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public sealed class BinaryGrainStateSerializer : IGrainStateSerializer
    {
        private readonly SerializationManager serializationManager;

        public BinaryGrainStateSerializer(SerializationManager serializationManager)
        {
            this.serializationManager = serializationManager;
        }

        public void Deserialize(IGrainState grainState, JObject entityData)
        {
            grainState.State = serializationManager.DeserializeFromByteArray<object>((byte[])entityData["statedata"]);
        }

        public JObject Serialize(IGrainState grainState)
        {
            var byteArray = serializationManager.SerializeToByteArray(grainState.State);

            return new JObject(new JProperty("statedata", byteArray));
        }
    }
}
