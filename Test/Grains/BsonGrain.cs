using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class BsonGrainGrain : IBSonGrain
    {
        private readonly IPersistentState<BsonGrainState> state;

        public BsonGrainGrain([PersistentState(nameof(BsonGrainState), "MongoDBBsonStore")] IPersistentState<BsonGrainState> state)
        {
            this.state = state;
        }

        public async Task PersistAsync(string name)
        {
            state.State.Name = name;
            state.State.Time = DateTime.UtcNow;
            state.State.Guid = Guid.NewGuid();
            await state.WriteStateAsync();
        }
    }
}