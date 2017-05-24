namespace Orleans.Providers.MongoDB.Test.Grains
{
    using System.Threading.Tasks;

    using Orleans.Providers.MongoDB.Test.GrainInterfaces;

    [StorageProvider(ProviderName = "MongoDBStore")]
    public class HelloWorldGrain : Grain, IHelloWorldGrain
    {
        public Task<string> SayHello(string name)
        {
            return Task.FromResult($"hello {name}");
        }
    }
}