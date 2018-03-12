using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public interface IGrainStateSerializer
    {
        JObject Serialize(IGrainState grainState);

        void Deserialize(IGrainState grainState, JObject entityData);
    }
}
