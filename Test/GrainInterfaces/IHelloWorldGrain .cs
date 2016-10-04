namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    using System.Threading.Tasks;

    public interface IHelloWorldGrain : IGrainWithIntegerKey
    {
        #region Public methods and operators

        Task<string> SayHello(string name);

        #endregion
    }
}