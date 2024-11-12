using System;
using MongoDB.Driver;
using MongoSandbox;
using Orleans.Providers.MongoDB.Utils;

namespace Orleans.Providers.MongoDB.UnitTest.Fixtures
{
    public class MongoDatabaseFixture : IDisposable
    {
        private bool disposedValue;

        private static readonly Lazy<IMongoRunner> _databaseRunner = new(() => MongoRunner.Run());
        private static readonly Lazy<IMongoRunner> _replicaSetRunner = new(() => MongoRunner.Run(new MongoRunnerOptions { UseSingleNodeReplicaSet = true }));

        public static string DatabaseConnectionString => _databaseRunner.Value.ConnectionString;

        public static string ReplicaSetConnectionString => _replicaSetRunner.Value.ConnectionString;

        public static IMongoClientFactory DatabaseFactory => new DefaultMongoClientFactory(new MongoClient(DatabaseConnectionString));

        public static IMongoClientFactory ReplicaSetFactory => new DefaultMongoClientFactory(new MongoClient(ReplicaSetConnectionString));

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
