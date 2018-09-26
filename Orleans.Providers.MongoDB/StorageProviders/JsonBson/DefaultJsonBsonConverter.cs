using System;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.StorageProviders
{

    /// <summary>
    /// The default implementation of IJsonBsonConverter. You can inherit this class as a quick way to provide 
    /// custom converters for specific grain types.
    /// </summary>
    public class DefaultJsonBsonConverter : IJsonBsonConverter
    {

        /// ============== To BSON =====================

        /// <inheritdoc />
        public virtual BsonDocument ToBson(JObject source)
        {
            var result = new BsonDocument();

            foreach (var property in source)
            {
                result.Add(EscapeJson(property.Key), property.Value.ToBson());
            }

            return result;
        }

        protected virtual BsonArray ToBson(JArray source)
        {
            var result = new BsonArray();

            foreach (var item in source)
            {
                result.Add(ToBson(item));
            }

            return result;
        }

        protected virtual BsonValue ToBson(JToken source)
        {
            switch (source.Type)
            {
                case JTokenType.Object:
                    return ToBson((JObject)source);
                case JTokenType.Array:
                    return ToBson((JArray)source);
                case JTokenType.Integer:
                    return BsonValue.Create(((JValue)source).Value);
                case JTokenType.Float:
                    return BsonValue.Create(((JValue)source).Value);
                case JTokenType.String:
                    return BsonValue.Create(((JValue)source).Value);
                case JTokenType.Boolean:
                    return BsonValue.Create(((JValue)source).Value);
                case JTokenType.Null:
                    return BsonNull.Value;
                case JTokenType.Undefined:
                    return BsonUndefined.Value;
                case JTokenType.Bytes:
                    return BsonValue.Create(((JValue)source).Value);
                case JTokenType.Guid:
                    return BsonValue.Create(((JValue)source).ToString());
                case JTokenType.Uri:
                    return BsonValue.Create(((JValue)source).ToString());
                case JTokenType.TimeSpan:
                    return BsonValue.Create(((JValue)source).ToString());
                case JTokenType.Date:
                {
                    var value = ((JValue)source).Value;

                    if (value is DateTime dateTime)
                    {
                        return dateTime.ToString("yyyy-MM-ddTHH:mm:ssK");
                    }
                    else if (value is DateTimeOffset dateTimeOffset)
                    {
                        return dateTimeOffset.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssK");
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
            }

            throw new NotSupportedException($"Cannot convert {source.GetType()} to Bson.");
        }

        protected virtual string EscapeJson(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            if (value[0] == '$')
            {
                return "__" + value.Substring(1);
            }

            return value;
        }



        /// ============== To JSON =====================

        /// <inheritdoc />
        public virtual JObject ToJToken(BsonDocument source)
        {
            var result = new JObject();

            foreach (var property in source)
            {
                result.Add(UnescapeBson(property.Name), ToJToken(property.Value));
            }

            return result;
        }

        protected virtual JArray ToJToken(BsonArray source)
        {
            var result = new JArray();

            foreach (var item in source)
            {
                result.Add(ToJToken(item));
            }

            return result;
        }
        protected virtual JToken ToJToken(BsonValue source)
        {
            switch (source.BsonType)
            {
                case BsonType.Document:
                    return ToJToken(source.AsBsonDocument);
                case BsonType.Array:
                    return ToJToken(source.AsBsonArray);
                case BsonType.Double:
                    return new JValue(source.AsDouble);
                case BsonType.String:
                    return new JValue(source.AsString);
                case BsonType.Boolean:
                    return new JValue(source.AsBoolean);
                case BsonType.DateTime:
                    return new JValue(source.ToUniversalTime());
                case BsonType.Int32:
                    return new JValue(source.AsInt32);
                case BsonType.Int64:
                    return new JValue(source.AsInt64);
                case BsonType.Decimal128:
                    return new JValue(source.AsDecimal);
                case BsonType.Binary:
                    return new JValue(source.AsBsonBinaryData.Bytes);
                case BsonType.Null:
                    return JValue.CreateNull();
                case BsonType.Undefined:
                    return JValue.CreateUndefined();
            }

            throw new NotSupportedException($"Cannot convert {source.GetType()} to Json.");
        }
        protected virtual string UnescapeBson(string value)
        {
            if (value.Length < 2)
            {
                return value;
            }

            if (value[0] == '_' && value[1] == '_')
            {
                return "$" + value.Substring(2);
            }

            return value;
        }
    }
}