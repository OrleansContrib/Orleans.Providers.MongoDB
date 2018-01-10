using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using Orleans.Providers.MongoDB.Utils;

namespace Orleans.Providers.MongoDB.StorageProviders.V2
{
    public class BsonJsonSerializer<T> : ClassSerializerBase<T> where T : class
    {
        private readonly JsonSerializer serializer;

        public BsonJsonSerializer(JsonSerializer serializer)
        {
            Guard.NotNull(serializer, nameof(serializer));

            this.serializer = serializer;
        }

        protected override T DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var jsonReader = new BsonJsonReader(context.Reader);

            return serializer.Deserialize<T>(jsonReader);
        }

        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, T value)
        {
            var jsonWriter = new BsonJsonWriter(context.Writer);

            serializer.Serialize(jsonWriter, value);
        }
    }
}
