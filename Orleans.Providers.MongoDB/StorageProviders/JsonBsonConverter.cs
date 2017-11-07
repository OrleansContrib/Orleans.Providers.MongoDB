﻿using System;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public static class JsonBsonConverter
    {
        public static BsonDocument ToBson(this JObject source)
        {
            var result = new BsonDocument();

            foreach (var property in source)
            {
                var name = Escape(property.Key);

                result.Add(name, property.Value.ToBson());
            }

            return result;
        }

        public static JObject ToJToken(this BsonDocument source)
        {
            var result = new JObject();

            foreach (var property in source)
            {
                var key = Unescape(property.Name);

                result.Add(key, property.Value.ToJToken());
            }

            return result;
        }

        public static BsonArray ToBson(this JArray source)
        {
            var result = new BsonArray();

            foreach (var item in source)
            {
                result.Add(item.ToBson());
            }

            return result;
        }

        public static JArray ToJToken(this BsonArray source)
        {
            var result = new JArray();

            foreach (var item in source)
            {
                result.Add(item.ToJToken());
            }

            return result;
        }

        public static BsonValue ToBson(this JToken source)
        {
            switch (source.Type)
            {
                case JTokenType.Object:
                    return ((JObject)source).ToBson();
                case JTokenType.Array:
                    return ((JArray)source).ToBson();
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
                case JTokenType.Date:
                    return BsonValue.Create(((JValue)source).Value.ToString());
                case JTokenType.Bytes:
                    return BsonValue.Create(((JValue)source).Value);
                case JTokenType.Guid:
                    return BsonValue.Create(((JValue)source).Value.ToString());
                case JTokenType.Uri:
                    return BsonValue.Create(((JValue)source).Value.ToString());
                case JTokenType.TimeSpan:
                    return BsonValue.Create(((JValue)source).Value.ToString());
            }

            throw new NotSupportedException($"Cannot convert {source.GetType()} to Bson.");
        }

        public static JToken ToJToken(this BsonValue source)
        {
            switch (source.BsonType)
            {
                case BsonType.Document:
                    return source.AsBsonDocument.ToJToken();
                case BsonType.Array:
                    return source.AsBsonArray.ToJToken();
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

        private static string Escape(string value)
        {
            return ReplaceFirstCharacter(value, '$', '§');
        }

        private static string Unescape(string value)
        {
            return ReplaceFirstCharacter(value, '§', '$');
        }

        private static string ReplaceFirstCharacter(string value, char source, char target)
        {
            if (value.Length == 0)
            {
                return value;
            }

            if (value[0] == source)
            {
                return target + value.Substring(1);
            }

            return value;
        }
    }
}