using System;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class BsonGrainState
    {
        public string Name { get; set; }

        public Guid Guid { get; set; }

        public DateTime Time { get; set; }
    }
}
