using System;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    ///     Orleans storage provider implementation for file-backed stores.
    /// </summary>
    /// <remarks>
    ///     The storage provider should be included in a deployment by adding this line to the Orleans server configuration
    ///     file:
    ///     <Provider Type="Samples.StorageProviders.OrleansFileStorage" Name="FileStore" RooDirectory="SOME FILE PATH" />
    ///     and this line to any grain that uses it:
    ///     [StorageProvider(ProviderName = "FileStore")]
    ///     The name 'FileStore' is an arbitrary choice.
    ///     Note that unless the root directory path is a network path available to all silos in a deployment, grain state
    ///     will not transport from one silo to another.
    /// </remarks>
    public class FileStorageProvider : BaseJSONStorageProvider
    {
        /// <summary>
        ///     The directory path, relative to the host of the silo. Set from
        ///     configuration data during initialization.
        /// </summary>
        public string RootDirectory { get; set; }

        /// <summary>
        ///     Initializes the provider during silo startup.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;
            RootDirectory = config.Properties["RootDirectory"];
            if (string.IsNullOrWhiteSpace(RootDirectory)) throw new ArgumentException("RootDirectory property not set");
            DataManager = new FileDataManager(RootDirectory);
            return base.Init(name, providerRuntime, config);
        }
    }
}