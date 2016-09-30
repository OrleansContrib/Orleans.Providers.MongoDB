namespace Orleans.Providers.MongoDB.Test.Host
{
    #region Using

    using System;
    using System.Net;

    using Orleans.Runtime.Configuration;
    using Orleans.Runtime.Host;

    #endregion

    /// <summary>
    /// The orleans host wrapper.
    /// </summary>
    internal class OrleansHostWrapper : IDisposable
    {
        #region Fields

        /// <summary>
        /// The silo host.
        /// </summary>
        private SiloHost siloHost;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OrleansHostWrapper"/> class.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public OrleansHostWrapper(string[] args)
        {
            this.ParseArguments(args);
            this.Init();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to debug.
        /// </summary>
        public bool Debug
        {
            get
            {
                return this.siloHost != null && this.siloHost.Debug;
            }

            set
            {
                this.siloHost.Debug = value;
            }
        }

        #endregion

        #region Public methods and operators

        /// <summary>
        /// The dispose method.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// The print usage.
        /// </summary>
        public void PrintUsage()
        {
            Console.WriteLine(@"USAGE: 
    orleans host [<siloName> [<configFile>]] [DeploymentId=<idString>] [/debug]
Where:
    <siloName>      - Name of this silo in the Config file list (optional)
    DeploymentId=<idString> 
                    - Which deployment group this host instance should run in (optional)");
        }

        /// <summary>
        /// The method used to run the application.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        /// <exception cref="SystemException">
        /// </exception>
        public bool Run()
        {
            bool ok = false;

            try
            {
                this.siloHost.InitializeOrleansSilo();

                ok = this.siloHost.StartOrleansSilo();

                if (ok)
                {
                    Console.WriteLine(
                        string.Format(
                            "Successfully started Orleans silo '{0}' as a {1} node.",
                            this.siloHost.Name,
                            this.siloHost.Type));
                }
                else
                {
                    throw new SystemException(
                        string.Format(
                            "Failed to start Orleans silo '{0}' as a {1} node.",
                            this.siloHost.Name,
                            this.siloHost.Type));
                }
            }
            catch (Exception exc)
            {
                this.siloHost.ReportStartupError(exc);
                var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        /// <summary>
        /// The method used to stop the application.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Stop()
        {
            bool ok = false;

            try
            {
                this.siloHost.StopOrleansSilo();

                Console.WriteLine(string.Format("Orleans silo '{0}' shutdown.", this.siloHost.Name));
            }
            catch (Exception exc)
            {
                this.siloHost.ReportStartupError(exc);
                var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        #endregion

        #region Other Methods

        /// <summary>
        /// The dispose method.
        /// </summary>
        /// <param name="dispose">
        /// The dispose.
        /// </param>
        protected virtual void Dispose(bool dispose)
        {
            this.siloHost.Dispose();
            this.siloHost = null;
        }

        /// <summary>
        /// The init method.
        /// </summary>
        private void Init()
        {
            this.siloHost.LoadOrleansConfig();
        }

        /// <summary>
        /// The parse arguments method.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool ParseArguments(string[] args)
        {
            string deploymentId = null;

            string siloName = Dns.GetHostName(); // Default to machine name

            int argPos = 1;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a.StartsWith("-") || a.StartsWith("/"))
                {
                    switch (a.ToLowerInvariant())
                    {
                        case "/?":
                        case "/help":
                        case "-?":
                        case "-help":

                            // Query usage help
                            return false;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }
                else if (a.Contains("="))
                {
                    string[] split = a.Split('=');
                    if (string.IsNullOrEmpty(split[1]))
                    {
                        Console.WriteLine("Bad command line arguments supplied: " + a);
                        return false;
                    }

                    switch (split[0].ToLowerInvariant())
                    {
                        case "deploymentid":
                            deploymentId = split[1];
                            break;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }

                // unqualified arguments below
                else if (argPos == 1)
                {
                    siloName = a;
                    argPos++;
                }
                else
                {
                    // Too many command line arguments
                    Console.WriteLine("Too many command line arguments supplied: " + a);
                    return false;
                }
            }

            var config = ClusterConfiguration.LocalhostPrimarySilo();
            config.AddMemoryStorageProvider();
            this.siloHost = new SiloHost(siloName, config);

            if (deploymentId != null)
            {
                this.siloHost.DeploymentId = deploymentId;
            }

            return true;
        }

        #endregion
    }
}