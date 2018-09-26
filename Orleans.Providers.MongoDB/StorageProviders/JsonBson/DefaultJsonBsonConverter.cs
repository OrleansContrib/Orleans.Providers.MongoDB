using System;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.StorageProviders {

    /// <summary>
    /// The default implementation of IJsonBsonConverter. You can inherit this class as a quick way to provide 
    /// custom converters for specific grain types.
    /// </summary>
    public class DefaultJsonBsonConverter : IJsonBsonConverter {

        /// ============== To BSON =====================

        /// <inheritdoc />
        public virtual BsonDocument ToBson(JObject source) {
            var result = new BsonDocument();

            foreach (var property in source) {
                result.Add(EscapeJson(property.Key), property.Value.ToBson());
            }

            return result;
        }
        protected virtual BsonArray ToBson(JArray source) {
            var result = new BsonArray();

            foreach (var item in source) {
                result.Add(ToBson(item));
            }

            return result;
        }
        protected virtual BsonValue ToBson(JToken source) {
            switch (source.Type) {
                case JTokenType.Object:
                    return JTokenObjectToBson(source);
                case JTokenType.Array:
                    return JTokenArrayToBson(source);
                case JTokenType.Integer:
                    return JTokenIntegerToBson(source);
                case JTokenType.Float:
                    return JTokenFloatToBson(source);
                case JTokenType.String:
                    return JTokenStringToBson(source);
                case JTokenType.Boolean:
                    return JTokenBoolToBson(source);
                case JTokenType.Null:
                    return JTokenNullToBson(source);
                case JTokenType.Undefined:
                    return JTokenUndefinedToBson(source);
                case JTokenType.Bytes:
                    return JTokenBytesToBson(source);
                case JTokenType.Guid:
                    return JTokenGuidToBson(source);
                case JTokenType.Uri:
                    return JTokenUriToBson(source);
                case JTokenType.TimeSpan:
                    return JTokenTimeSpanToBson(source);
                case JTokenType.Date:
                    return JTokenDateToBson(source);
                default:
                    throw new NotImplementedException($"{nameof(JToken)} to {nameof(BsonValue)} conversion is not implemented for {nameof(JTokenType)} '{source.Type}'.");
            }
        }
        protected virtual BsonValue JTokenObjectToBson(JToken source) => ToBson((JObject)source);
        protected virtual BsonValue JTokenArrayToBson(JToken source) => ToBson((JArray)source);
        protected virtual BsonValue JTokenIntegerToBson(JToken source) => BsonValue.Create(((JValue)source).Value);
        protected virtual BsonValue JTokenFloatToBson(JToken source) => BsonValue.Create(((JValue)source).Value);
        protected virtual BsonValue JTokenStringToBson(JToken source) => BsonValue.Create(((JValue)source).Value);
        protected virtual BsonValue JTokenBoolToBson(JToken source) => BsonValue.Create(((JValue)source).Value);
        protected virtual BsonValue JTokenNullToBson(JToken source) => BsonNull.Value;
        protected virtual BsonValue JTokenUndefinedToBson(JToken source) => BsonUndefined.Value;
        protected virtual BsonValue JTokenBytesToBson(JToken source) => BsonValue.Create(((JValue)source).Value);
        protected virtual BsonValue JTokenGuidToBson(JToken source) => BsonValue.Create(((JValue)source).ToString());
        protected virtual BsonValue JTokenUriToBson(JToken source) => BsonValue.Create(((JValue)source).ToString());
        protected virtual BsonValue JTokenTimeSpanToBson(JToken source) => BsonValue.Create(((JValue)source).ToString());
        protected virtual BsonValue JTokenDateToBson(JToken source) {
            var value = ((JValue)source).Value;
            if (value is DateTime dateTime) {
                return dateTime.ToString("yyyy-MM-ddTHH:mm:ssK");
            } else if (value is DateTimeOffset dateTimeOffset) {
                return dateTimeOffset.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssK");
            } else {
                return value.ToString();
            }
        }
        protected virtual string EscapeJson(string value) {
            if (value.Length == 0) {
                return value;
            }

            if (value[0] == '$') {
                return "__" + value.Substring(1);
            }

            return value;
        }

        /// ============== To JSON =====================

        /// <inheritdoc />
        public virtual JObject ToJToken(BsonDocument source) {
            var result = new JObject();

            foreach (var property in source) {
                result.Add(UnescapeBson(property.Name), ToJToken(property.Value));
            }

            return result;
        }
        protected virtual JArray ToJToken(BsonArray source) {
            var result = new JArray();

            foreach (var item in source) {
                result.Add(ToJToken(item));
            }

            return result;
        }
        protected virtual JToken ToJToken(BsonValue source) {
            switch (source.BsonType) {
                case BsonType.Document:
                    return BsonDocumentToJToken(source);
                case BsonType.Array:
                    return BsonArrayToJToken(source);
                case BsonType.Double:
                    return BsonDoubleToJToken(source);
                case BsonType.String:
                    return BsonStringToJToken(source);
                case BsonType.Boolean:
                    return BsonBoolToJToken(source);
                case BsonType.DateTime:
                    return BsonDateTimeToJToken(source);
                case BsonType.Int32:
                    return BsonInt32ToJToken(source);
                case BsonType.Int64:
                    return BsonInt64ToJToken(source);
                case BsonType.Decimal128:
                    return BsonDecimal128ToJToken(source);
                case BsonType.Binary:
                    return BsonBinaryToJToken(source);
                case BsonType.Null:
                    return BsonNullToJToken();
                case BsonType.Undefined:
                    return BsonUndefinedToJToken();
                default:
                    throw new NotImplementedException($"{nameof(BsonValue)} to {nameof(JToken)} conversion is not implemented for {nameof(BsonType)} '{source.BsonType}'.");
            }
        }
        protected virtual JToken BsonDocumentToJToken(BsonValue source) => ToJToken(source.AsBsonDocument);
        protected virtual JToken BsonArrayToJToken(BsonValue source) => ToJToken(source.AsBsonArray);
        protected virtual JToken BsonDoubleToJToken(BsonValue source) => new JValue(source.AsDouble);
        protected virtual JToken BsonStringToJToken(BsonValue source) => new JValue(source.AsString);
        protected virtual JToken BsonBoolToJToken(BsonValue source) => new JValue(source.AsBoolean);
        protected virtual JToken BsonDateTimeToJToken(BsonValue source) => new JValue(source.ToUniversalTime());
        protected virtual JToken BsonInt32ToJToken(BsonValue source) => new JValue(source.AsInt32);
        protected virtual JToken BsonInt64ToJToken(BsonValue source) => new JValue(source.AsInt64);
        protected virtual JToken BsonDecimal128ToJToken(BsonValue source) => new JValue(source.AsDecimal);
        protected virtual JToken BsonBinaryToJToken(BsonValue source) => new JValue(source.AsBsonBinaryData.Bytes);
        protected virtual JToken BsonNullToJToken() => JValue.CreateNull();
        protected virtual JToken BsonUndefinedToJToken() => JValue.CreateUndefined();
        protected virtual string UnescapeBson(string value) {
            if (value.Length < 2) {
                return value;
            }

            if (value[0] == '_' && value[1] == '_') {
                return "$" + value.Substring(2);
            }

            return value;
        }
    }
}