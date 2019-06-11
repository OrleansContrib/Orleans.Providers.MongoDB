using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;

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

        public Task AddVacationLeave()
        {
            var identifier = State.EmployeeLeave.Count + 1;
            State.EmployeeLeave.Add(new VacationLeave(identifier));
            return WriteStateAsync();
        }

        public Task AddSickLeave()
        {
            var idenitifier = State.EmployeeLeave.Count + 1;
            State.EmployeeLeave.Add(new SickLeave(idenitifier));
            return WriteStateAsync();
        }

        public Task<int> ReturnLevelWithoutReadState()
        {
            return Task.FromResult(State.Level);
        }

        public Task<int> ReturnLeaveCountWithoutReadStateAsync()
        {
            return Task.FromResult(State.EmployeeLeave.Count);
        }

        public async Task<int> ReturnLeaveCountUsingReadState()
        {
            await ReadStateAsync();
            return State.EmployeeLeave.Count;
        }
    }
}