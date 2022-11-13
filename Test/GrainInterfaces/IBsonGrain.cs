using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IBsonGrain : IGrainWithStringKey
    {
        Task PersistAsync(string name);
    }
}