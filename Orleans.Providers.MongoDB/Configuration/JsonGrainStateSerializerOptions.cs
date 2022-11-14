using System;
using Newtonsoft.Json;

namespace Orleans.Providers.MongoDB.Configuration
{
    public class JsonGrainStateSerializerOptions
    {
        public Action<JsonSerializerSettings> ConfigureJsonSerializerSettings { get; set; } = ConfigureDefaultSettings;

        private static void ConfigureDefaultSettings(JsonSerializerSettings settings)
        {
            //// https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/44
            //// Always include the default value, so that the deserialization process can overwrite default
            //// values that are not equal to the system defaults.
            settings.NullValueHandling = NullValueHandling.Include;
            settings.DefaultValueHandling = DefaultValueHandling.Populate;
        }
    }
}
