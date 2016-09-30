namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    #region Using

    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// The HelloWorldGrain interface.
    /// </summary>
    public interface IHelloWorldGrain : IGrainWithIntegerKey
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
        Task<string> SayHello(string name);

        #endregion
    }
}