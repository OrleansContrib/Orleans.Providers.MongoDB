using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    [StorageProvider(ProviderName = "MongoDBStore")]
    public class EmployeeGrain : Grain<EmployeeState>, IEmployeeGrain
    {
        #region Overrides of Grain

        /// <summary>
        ///     This method is called at the end of the process of activating a grain.
        ///     It is called before any messages have been dispatched to the grain.
        ///     For grains with declared persistent state, this method is called after the State property has been populated.
        /// </summary>
        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        #endregion

        #region Implementation of IEmployeeGrain

        public Task SetLevel(int level)
        {
            State.Level = level;
            return WriteStateAsync();
        }

        public async Task<int> ReturnLevel()
        {
            await ReadStateAsync();
            return State.Level;
        }

        #endregion
    }
}