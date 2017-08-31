using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IHelloWorldGrain : IGrainWithIntegerKey
    {
        #region Public methods and operators

        Task<string> SayHello(string name);

        #endregion
    }
}