using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Orleans.Providers.MongoDB.StorageProviders.Serializers.BsonSerializationProviders;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializers
{
    public class BsonGrainStateSerializer : IGrainStateSerializer
    {
        public BsonGrainStateSerializer()
        {
            BsonSerializer.RegisterSerializationProvider(new OrleansBsonSerializationProvider());
        }

        public BsonValue Serialize<T>(T state)
        {
            return BsonDocumentWrapper.Create(state);
        }

        public T Deserialize<T>(BsonValue value)
        {
            if (value.IsBsonDocument)
            {
                return BsonSerializer.Deserialize<T>(value.AsBsonDocument);
            }
            else
            {
                var document = new BsonDocument("value", value);
                var wrapper = BsonSerializer.Deserialize<ValueWrapper<T>>(document);
                return wrapper.Value;
            }
        }

        private class ValueWrapper<T>
        {
            [BsonElement("value")]
            public T Value { get; set; }
        }
    }
}
