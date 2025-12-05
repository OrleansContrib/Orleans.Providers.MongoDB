using System;
using System.Threading;
using MongoDB.Driver;
using MongoSandbox;
using Orleans.Providers.MongoDB.Utils;

namespace Orleans.Providers.MongoDB.UnitTest.Fixtures
{
    public class MongoDatabaseFixture : IDisposable
    {
        private bool disposedValue;

        private static readonly Lazy<IMongoRunner> _databaseRunner = new(
            () => MongoRunner.Run(),
            LazyThreadSafetyMode.ExecutionAndPublication
        );
        private static readonly Lazy<IMongoRunner> _replicaSetRunner = new(
            () => MongoRunner.Run(new MongoRunnerOptions { UseSingleNodeReplicaSet = true }),
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        public static string DatabaseConnectionString => _databaseRunner.Value.ConnectionString;

        public static string ReplicaSetConnectionString => _replicaSetRunner.Value.ConnectionString;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_databaseRunner.IsValueCreated)
                    {
                        _databaseRunner.Value.Dispose();
                    }

                    if (_replicaSetRunner.IsValueCreated)
                    {
                        _replicaSetRunner.Value.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
