using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public interface IGrainStateSerializer
    {
        JObject Serialize<T>(IGrainState<T> grainState);

        void Deserialize<T>(IGrainState<T> grainState, JObject entityData);
    }
}
