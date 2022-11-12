using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MongoGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly ConcurrentDictionary<string, MongoGrainStorageCollection> collections = new ConcurrentDictionary<string, MongoGrainStorageCollection>();
        private readonly MongoDBGrainStorageOptions options;
        private readonly IMongoClient mongoClient;
        private readonly ILogger<MongoGrainStorage> logger;
        private readonly IGrainStateSerializer serializer;
        private IMongoDatabase database;

        public MongoGrainStorage(
            IMongoClientFactory mongoClientFactory,
            ILogger<MongoGrainStorage> logger,
            IGrainStateSerializer serializer,
            MongoDBGrainStorageOptions options)
        {
            this.mongoClient = mongoClientFactory.Create(options, "Storage");
            this.logger = logger;
            this.options = options;
            this.serializer = serializer;
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe<MongoGrainStorage>(ServiceLifecycleStage.ApplicationServices, Init);
        }

        private Task Init(CancellationToken ct)
        {
            return DoAndLog(nameof(Init), () =>
            {
                database = mongoClient.GetDatabase(options.DatabaseName);

                return Task.CompletedTask;
            });
        }

        public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return DoAndLog(nameof(ReadStateAsync), () =>
            {
                return GetCollection<T>(stateName, grainId).ReadAsync(grainId, grainState);
            });
        }
        public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return DoAndLog(nameof(WriteStateAsync), () =>
            {
                return GetCollection<T>(stateName, grainId).WriteAsync(grainId, grainState);
            });
        }

        public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return DoAndLog(nameof(ClearStateAsync), () =>
            {
                return GetCollection<T>(stateName, grainId).ClearAsync(grainId, grainState);
            });
        }

        private MongoGrainStorageCollection GetCollection<T>(string stateName, GrainId grainId)
        {
            var collectionName = $"{options.CollectionPrefix}{ReturnGrainName<T>(stateName, grainId)}";

            return collections.GetOrAdd(stateName, x =>
                new MongoGrainStorageCollection(
                    mongoClient,
                    options.DatabaseName,
                    collectionName,
                    options.CollectionConfigurator,
                    options.CreateShardKeyForCosmos,
                    serializer,
                    options.KeyGenerator));
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            logger.LogDebug($"{nameof(MongoGrainStorage)}.{actionName} called.");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogError((int)MongoProviderErrorCode.GrainStorageOperations, ex, $"{nameof(MongoGrainStorage)}.{actionName} failed. Exception={ex.Message}");

                throw;
            }
        }

        protected virtual string ReturnGrainName<T>(string stateName, GrainId grainId)
        {
            return stateName != "state" ? stateName : typeof(T).Name;
        }
    }
}
