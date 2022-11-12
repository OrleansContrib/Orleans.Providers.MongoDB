using Newtonsoft.Json.Linq;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public sealed class BinaryGrainStateSerializer : IGrainStateSerializer
    {
        private readonly OrleansGrainStorageSerializer serializer;

        public BinaryGrainStateSerializer(OrleansGrainStorageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public void Deserialize<T>(IGrainState<T> grainState, JObject entityData)
        {
            grainState.State = serializer.Deserialize<T>((byte[])entityData["statedata"]);
        }

        public JObject Serialize<T>(IGrainState<T> grainState)
        {
            var byteArray = serializer.Serialize<T>(grainState.State);
            return new JObject(new JProperty("statedata", byteArray));
        }
    }
}
