using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IStreamProducerGrain : IGrainWithIntegerKey
    {
        Task ProduceEvents();
    }
}
