using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IHelloWorldGrain : IGrainWithIntegerKey
    {
        Task<string> SayHello(string name);
    }
}