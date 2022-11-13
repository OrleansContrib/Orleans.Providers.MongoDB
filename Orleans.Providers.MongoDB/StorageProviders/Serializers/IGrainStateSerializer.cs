using MongoDB.Bson;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public interface IGrainStateSerializer
    {
        BsonValue Serialize<T>(T state);

        T Deserialize<T>(BsonValue value);
    }
}
