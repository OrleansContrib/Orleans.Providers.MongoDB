using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IStreamConsumerGrain : IGrainWithIntegerKey
    {
        Task Activate();

        Task<int> GetConsumedItems();
    }
}
