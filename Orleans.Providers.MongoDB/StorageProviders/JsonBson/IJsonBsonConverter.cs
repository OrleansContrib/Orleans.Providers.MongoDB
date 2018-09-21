using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    /// Grainstate is first converted to json using the Orleans default Json serializer, and then it is converted to Bson
    /// for storage in MongoDB. Inherit this interface to provide custom json-bson conversion for your grain state.
    /// </summary>
    public interface IJsonBsonConverter
    {
        /// <summary>
        /// Converts the Orleans Json serializer's JObject representation of Grain state into BSON format,
        /// ready for writing to MongoDB storage.
        /// </summary>
        BsonDocument ToBson(JObject source);

        /// <summary>
        /// Converts MongoDB Bson storage format grain state back into a JObject ready for deserialiation into a GrainState object
        /// by the Orleans Json serializer.
        /// </summary>
        JObject ToJToken(BsonDocument source);
    }
}