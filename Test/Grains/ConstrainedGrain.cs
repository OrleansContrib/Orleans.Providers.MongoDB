using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    internal class ConstrainedGrain : Grain, IConstrainedGrain
    {
        private readonly IPersistentState<ConstrainedGrainState> state;

        public ConstrainedGrain([PersistentState(nameof(ConstrainedGrain), "MongoDBStore")] IPersistentState<ConstrainedGrainState> state)
        {
            this.state = state;
        }

        public async Task SetName(string name)
        {
            state.State.Name = name;

            try
            {
                await state.WriteStateAsync();
            }
            catch (Exception ex)
            {
                throw new ProviderStateException(ex.Message);
            }
        }
    }
}