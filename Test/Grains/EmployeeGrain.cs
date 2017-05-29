using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    using Orleans.Providers.MongoDB.Test.GrainInterfaces;

    [StorageProvider(ProviderName = "MongoDBStore")]
    public class EmployeeGrain : Grain<EmployeeState>, IEmployeeGrain
    {
        #region Implementation of IEmployeeGrain

        public Task SetLevel(int level)
        {
            this.State.Level = level;
            return base.WriteStateAsync();
        }

        public async Task<int> ReturnLevel()
        {
            await base.ReadStateAsync();
            return this.State.Level;
        }

        #endregion

        #region Overrides of Grain

        /// <summary>
        /// This method is called at the end of the process of activating a grain.
        /// It is called before any messages have been dispatched to the grain.
        /// For grains with declared persistent state, this method is called after the State property has been populated.
        /// </summary>
        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        #endregion
    }
}
