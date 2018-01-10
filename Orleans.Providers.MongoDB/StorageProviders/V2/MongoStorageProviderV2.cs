using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.StorageProviders.V2
{
    public class MongoStorageProviderV2 : IStorageProvider
    {
        public const string ConnectionStringProperty = "ConnectionString";
        public const string CollectionPrefixProperty = "CollectionPrefix";
        public const string DatabaseNameProperty = "DatabaseProperty";
        public const string SerializerSettingsProperty = "SerializerSettings";
        private readonly ILogger<MongoStorageProviderV2> logger;
        private readonly ConcurrentDictionary<(Type, string), IMongoStorageCollection> collections = new ConcurrentDictionary<(Type, string), IMongoStorageCollection>();
        private IMongoDatabase database;
        private string prefix;
        private JsonSerializerSettings serializerSettings;
        private JsonSerializer serializer;
        private SerializationManager serializationManager;

        public Logger Log { get; private set; }

        public string Name { get; private set; }

        public MongoStorageProviderV2(ILogger<MongoStorageProviderV2> logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;

            serializationManager = providerRuntime.ServiceProvider.GetRequiredService<SerializationManager>();
            serializerSettings = ReturnSerializerSettings(providerRuntime, config);
            serializer = JsonSerializer.Create(serializerSettings);

            var mongoConnectionString = config.GetProperty(ConnectionStringProperty, string.Empty);
            var mongoCollectionPrefix = config.GetProperty(CollectionPrefixProperty, string.Empty);
            var mongoDatabaseName = config.GetProperty(DatabaseNameProperty, string.Empty);

            prefix = mongoCollectionPrefix;

            var client = MongoClientPool.Instance(mongoConnectionString);

            database = client.GetDatabase(mongoDatabaseName);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var grainName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            try
            {
                var collection = GetCollection(typeof(object), grainType, grainReference);

                await collection.Delete(grainKey);
            }
            catch (Exception ex)
            {
                logger.LogError((int)MongoProviderErrorCode.StorageProvider_Deleting, $"Error Deleting: GrainType={grainType} GrainId={grainKey} ETag={grainState.ETag} from Collection={grainName} Exception={ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var grainName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            try
            {
                IMongoStorageCollection collection;

                if (grainState.State != null)
                {
                    collection = GetCollection(grainState.State.GetType(), grainType, grainReference);
                }
                else
                {
                    collection = GetCollection(typeof(object), grainType, grainReference);
                }

                grainState.ETag = await collection.Write(grainKey, grainState.State, grainState.ETag);
            }
            catch (Exception ex)
            {
                logger.LogError((int)MongoProviderErrorCode.StorageProvider_Writing, $"Error Writing: GrainType={grainType} GrainId={grainKey} ETag={grainState.ETag} to Collection={grainName} Exception={ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (grainState.State == null)
            {
                throw new InvalidOperationException("Can only read to valid grain state. Initialize with default value.");
            }

            var grainTypeName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            try
            {
                var collection = GetCollection(grainState.State.GetType(), grainType, grainReference);

                var (Etag, Value) = await collection.Read(grainKey);

                if (Value != null)
                {
                    grainState.State = Value;
                    grainState.ETag = Etag;
                }
            }
            catch (Exception ex)
            {
                logger.LogError((int)MongoProviderErrorCode.StorageProvider_Reading, $"Error Reading: GrainType={grainType} GrainId={grainKey} ETag={grainState.ETag} from Collection={grainTypeName} Exception={ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public Task Close()
        {
            return Task.CompletedTask;
        }

        protected virtual JsonSerializerSettings ReturnSerializerSettings(IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            return OrleansJsonSerializer.UpdateSerializerSettings(OrleansJsonSerializer.GetDefaultSerializerSettings(serializationManager, providerRuntime.GrainFactory), config);
        }

        protected virtual string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return grainType.Split('.', '+').Last();
        }

        private IMongoStorageCollection GetCollection(Type type, string grainName, GrainReference grainReference)
        {
            var key = (type, grainName);

            return collections.GetOrAdd(key, x => (IMongoStorageCollection)Activator.CreateInstance(typeof(MongoStorageCollection<>).MakeGenericType(type), database, prefix + grainName));
        }
    }
}
