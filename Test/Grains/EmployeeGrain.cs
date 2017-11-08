namespace Orleans.Providers.MongoDB.Test.Grains
{
    [StorageProvider(ProviderName = "MongoDBStore")]
    public class EmployeeGrain : Grain<EmployeeState>, IEmployeeGrain
    {
        /// <summary>
        ///     This method is called at the end of the process of activating a grain.
        ///     It is called before any messages have been dispatched to the grain.
        ///     For grains with declared persistent state, this method is called after the State property has been populated.
        /// </summary>
        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

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
    }
}