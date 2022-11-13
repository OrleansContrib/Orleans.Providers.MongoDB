using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IBSonGrain : IGrainWithStringKey
    {
        Task PersistAsync(string name);
    }
}