using System;
using System.Net;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;

namespace Orleans.Providers.MongoDB.Test.Host
{
    internal class OrleansHostWrapper : IDisposable
    {
        private SiloHost siloHost;

        public OrleansHostWrapper(string[] args)
        {
            ParseArguments(args);
            Init();
        }

        public bool Debug
        {
            get => siloHost != null && siloHost.Debug;

            set => siloHost.Debug = value;
        }

        public void Dispose()
        {
            Dispose(true);
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
            var ok = false;

            try
            {
                siloHost.InitializeOrleansSilo();

                ok = siloHost.StartOrleansSilo();

                if (ok)
                {
                    Console.WriteLine("Successfully started Orleans silo '{0}' as a {1} node.", siloHost.Name,
                        siloHost.Type);
                }
                else
                {
                    throw new Exception(
                        string.Format(
                            "Failed to start Orleans silo '{0}' as a {1} node.",
                            siloHost.Name,
                            siloHost.Type));
                }
            }
            catch (Exception exc)
            {
                siloHost.ReportStartupError(exc);
                var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        public bool Stop()
        {
            var ok = false;

            try
            {
                siloHost.StopOrleansSilo();

                Console.WriteLine("Orleans silo '{0}' shutdown.", siloHost.Name);
            }
            catch (Exception exc)
            {
                siloHost.ReportStartupError(exc);
                var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        protected virtual void Dispose(bool dispose)
        {
            siloHost.Dispose();
            siloHost = null;
        }

        private void Init()
        {
            siloHost.LoadOrleansConfig();
        }

        private bool ParseArguments(string[] args)
        {
            string deploymentId = null;

            var siloName = Dns.GetHostName(); // Default to machine name

            var argPos = 1;
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
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
                    var split = a.Split('=');
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
            siloHost = new SiloHost(siloName, config);

            if (deploymentId != null)
            {
                siloHost.DeploymentId = deploymentId;
            }

            return true;
        }
    }
}