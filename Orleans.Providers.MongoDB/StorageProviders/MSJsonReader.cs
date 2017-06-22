using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MSJsonReader : JsonTextReader
    {
        public override bool Read()
        {
            var hasToken = base.Read();

            if (hasToken && base.TokenType == JsonToken.PropertyName && base.Value != null && base.Value.Equals("__type"))
                base.SetToken(JsonToken.PropertyName, "$type");

            return hasToken;
        }

        public MSJsonReader(TextReader reader) : base(reader)
        {
        }
    }
}
