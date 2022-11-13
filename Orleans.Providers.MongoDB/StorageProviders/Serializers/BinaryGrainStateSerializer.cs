using MongoDB.Bson;
using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public sealed class BinaryGrainStateSerializer : IGrainStateSerializer
    {
        private const string BinaryElementName = "statedata";

        private readonly OrleansGrainStorageSerializer serializer;

        public BinaryGrainStateSerializer(Serializer serializer)
        {
            this.serializer = new OrleansGrainStorageSerializer(serializer);
        }

        public T Deserialize<T>(BsonValue value)
        {
            return serializer.Deserialize<T>(value[BinaryElementName].AsByteArray);
        }

        public BsonValue Serialize<T>(T state)
        {
            var binaryData = serializer.Serialize(state);
            return new BsonDocument(BinaryElementName, new BsonBinaryData(binaryData.ToArray()));
        }
    }
}
