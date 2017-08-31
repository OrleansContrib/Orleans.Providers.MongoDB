using System.IO;
using Newtonsoft.Json;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MSJsonReader : JsonTextReader
    {
        public MSJsonReader(TextReader reader) : base(reader)
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