namespace Orleans.Providers.MongoDB.Test.Host
{
    using System;
    using System.Net;

    using Orleans.Runtime.Configuration;
    using Orleans.Runtime.Host;

    internal class OrleansHostWrapper : IDisposable
    {
        private SiloHost siloHost;
        public OrleansHostWrapper(string[] args)
        {
            this.ParseArguments(args);
            this.Init();
        }

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
        public void Dispose()
        {
            this.Dispose(true);
        }

        public void PrintUsage()
        {
            Console.WriteLine(@"USAGE: 
    orleans host [<siloName> [<configFile>]] [DeploymentId=<idString>] [/debug]
Where:
    <siloName>      - Name of this silo in the Config file list (optional)
    DeploymentId=<idString> 
                    - Which deployment group this host instance should run in (optional)");
        }

        public bool Run()
        {
            bool ok = false;

            try
            {
                this.siloHost.InitializeOrleansSilo();

                ok = this.siloHost.StartOrleansSilo();

                if (ok)
                {
                    Console.WriteLine("Successfully started Orleans silo '{0}' as a {1} node.", this.siloHost.Name, this.siloHost.Type);
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

        public bool Stop()
        {
            bool ok = false;

            try
            {
                this.siloHost.StopOrleansSilo();

                Console.WriteLine("Orleans silo '{0}' shutdown.", this.siloHost.Name);
            }
            catch (Exception exc)
            {
                this.siloHost.ReportStartupError(exc);
                var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        protected virtual void Dispose(bool dispose)
        {
            this.siloHost.Dispose();
            this.siloHost = null;
        }

        private void Init()
        {
            this.siloHost.LoadOrleansConfig();
        }

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

                if (a.Contains("="))
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
            config.LoadFromFile("OrleansConfiguration.xml");

            // var config = ClusterConfiguration.LocalhostPrimarySilo();
            // config.AddMemoryStorageProvider();

            // //MongoDB
            // var props = new Dictionary<string, string>();
            // props["Database"] = "orleanssamples";
            // props["ConnectionString"] = "mongodb://localhost:27017/";
            // config.Globals.RegisterStorageProvider<Samples.StorageProviders.MongoDBStorage>("TestStore", props);
            this.siloHost = new SiloHost(siloName, config);

            if (deploymentId != null)
            {
                this.siloHost.DeploymentId = deploymentId;
            }

            return true;
        }
    }
}