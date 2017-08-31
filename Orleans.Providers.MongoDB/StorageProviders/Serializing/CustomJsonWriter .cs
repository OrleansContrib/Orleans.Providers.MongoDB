using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Orleans.Providers.MongoDB.StorageProviders.Serializing
{
    public class CustomJsonWriter : JsonTextWriter
    {
        public CustomJsonWriter(TextWriter textWriter) : base(textWriter)
        {
        }

        public override void WritePropertyName(string name, bool escape)
        {
            if (name == "$type") name = "__type";
            base.WritePropertyName(name, escape);
        }
    }

    public class CustomJsonReader : JsonTextReader
    {
        public CustomJsonReader(Stream readStream, Encoding effectiveEncoding)
            : base(new StreamReader(readStream, effectiveEncoding))
        {
        }

        public override bool Read()
        {
            var hasToken = base.Read();
            if (hasToken && TokenType == JsonToken.PropertyName && Value != null && Value.Equals("__type"))
                SetToken(JsonToken.PropertyName, "$type");
            return hasToken;
        }
    }
}