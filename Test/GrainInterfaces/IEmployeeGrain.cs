using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IEmployeeGrain : IGrainWithIntegerKey
    {
        Task SetLevel(int level);

        Task<int> ReturnLevel();

        Task AddVacationLeave();

        Task AddSickLeave();

        Task<int> ReturnLevelWithoutReadState();

        Task<int> ReturnLeaveCountWithoutReadStateAsync();

        Task<int> ReturnLeaveCountUsingReadState();
    }
}