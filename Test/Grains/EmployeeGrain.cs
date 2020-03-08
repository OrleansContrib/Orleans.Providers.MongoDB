using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    [StorageProvider(ProviderName = "MongoDBStore")]
    public class EmployeeGrain : Grain<EmployeeState>, IEmployeeGrain
    {
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

            State.EmployeeLeave.Add(new VacationLeave
            {
                Identifier = identifier,
                DateStart = DateTime.UtcNow.AddDays(identifier),
                DateEnd = DateTime.UtcNow.AddDays(identifier)
            });

            return WriteStateAsync();
        }

        public Task AddSickLeave()
        {
            var identifier = State.EmployeeLeave.Count + 1;

            State.EmployeeLeave.Add(new VacationLeave
            {
                Identifier = identifier,
                DateStart = DateTime.UtcNow.AddDays(identifier * -1),
                DateEnd = DateTime.UtcNow.AddDays(identifier * -1)
            });

            return WriteStateAsync();
        }

        public async Task<int> ReturnLeaveCount()
        {
            await ReadStateAsync();

            return State.EmployeeLeave.Count;
        }
    }
}