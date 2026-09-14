using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.UnitTest.Storage.TestGrains
{
    public interface IConstrainedGrain : IGrainWithIntegerKey
    {
        Task SetName(string name);
    }
}
