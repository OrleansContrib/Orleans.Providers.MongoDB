namespace Orleans.Providers.MongoDB.Test.Grains
{
    #region Using

    using System.Threading.Tasks;

    using Orleans.Providers.MongoDB.Test.GrainInterfaces;

    #endregion

    /// <summary>
    /// The hello world grain.
    /// </summary>
    public class HelloWorldGrain : Grain, IHelloWorldGrain
    {
        #region Public methods and operators

        /// <summary>
        /// The say hello.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public Task<string> SayHello(string name)
        {
            return Task.FromResult($"hello {name}");
        }

        #endregion
    }
}