using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IConstrainedGrain : IGrainWithIntegerKey
    {
        Task SetName(string name);
    }
}
