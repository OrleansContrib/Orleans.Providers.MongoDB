using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orleans.Providers.MongoDB.StorageProviders.V2
{
    public sealed class StorageDocument<T>
    {
        [BsonId]
        [BsonElement]
        [BsonRepresentation(BsonType.String)]
        public string Key { get; set; }

        [BsonRequired]
        [BsonElement]
        [BsonJson]
        public T State { get; set; }

        [BsonRequired]
        [BsonElement]
        public string Etag { get; set; }
    }
}
