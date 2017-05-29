using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IEmployeeGrain : IGrainWithIntegerKey
    {
        Task SetLevel(int level);

        Task<int> ReturnLevel();
    }
}
