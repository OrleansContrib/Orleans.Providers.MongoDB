using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public abstract class BaseJSONStorageProvider : IStorageProvider
    {
        public const string UseJsonFormatProperty = "UseJsonFormat";

        private JsonSerializerSettings serializerSettings;
        private JsonSerializer serializer;
        private SerializationManager serializationManager;
        
        protected IJSONStateDataManager DataManager { get; set; }

        /// <summary>
        ///     Use JSON or Binary serialization
        /// </summary>
        public bool UseJsonFormat { get; private set; }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public Logger Log { get; private set; }

        /// <inheritdoc />
        public virtual Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;

            Log = providerRuntime.GetLogger(GetType().FullName);

            serializationManager = providerRuntime.ServiceProvider.GetRequiredService<SerializationManager>();
            serializerSettings = 
                OrleansJsonSerializer.UpdateSerializerSettings(
                    OrleansJsonSerializer.GetDefaultSerializerSettings(serializationManager, providerRuntime.GrainFactory), config);
            serializer = JsonSerializer.Create(serializerSettings);

            UseJsonFormat = config.GetBoolProperty(UseJsonFormatProperty, true);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Close()
        {
            DataManager?.Dispose();
            DataManager = null;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            EnsureInitialized();

            var grainTypeName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            try
            {
                var result = await DataManager.Read(grainTypeName, grainKey);

                if (result.Value != null)
                {
                    ConvertFromStorageFormat(grainState, result.Value);

                    grainState.ETag = result.Etag;
                }
            }
            catch (Exception ex)
            {
                Log.Error((int) MongoProviderErrorCode.StorageProvider_Reading, $"Error Reading: GrainType={grainType} GrainId={grainKey} ETag={grainState.ETag} from Collection={grainTypeName} Exception={ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            EnsureInitialized();

            var grainTypeName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            try
            {
                var grainData = ConvertToStorageFormat(grainState);

                grainState.ETag = await DataManager.Write(grainTypeName, grainKey, grainData, grainState.ETag);
            }
            catch (Exception ex)
            {
                Log.Error((int)MongoProviderErrorCode.StorageProvider_Writing, $"Error Writing: GrainType={grainType} GrainId={grainKey} ETag={grainState.ETag} to Collection={grainTypeName} Exception={ex.Message}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            EnsureInitialized();

            var grainTypeName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            try
            {
                await DataManager.Delete(grainTypeName, grainKey);
            }
            catch (Exception ex)
            {
                Log.Error((int)MongoProviderErrorCode.StorageProvider_Deleting, $"Error Deleting: GrainType={grainType} GrainId={grainKey} ETag={grainState.ETag} from Collection={grainTypeName} Exception={ex.Message}", ex);
                throw;
            }
        }
        
        protected JObject ConvertToStorageFormat(IGrainState grainState)
        {
            if (UseJsonFormat)
            {
                var json = JObject.FromObject(grainState.State, serializer);

                return json;
            }
            else
            {
                var byteArray = serializationManager.SerializeToByteArray(grainState.State);

                return new JObject(new JProperty("statedata", byteArray));
            }
        }
        
        protected void ConvertFromStorageFormat(IGrainState grainState, JObject entityData)
        {
            if (UseJsonFormat)
            {
                var jsonReader = new JTokenReader(entityData);

                serializer.Populate(jsonReader, grainState.State);
            }
            else
            {
                grainState.State = serializationManager.DeserializeFromByteArray<object>((byte[]) entityData["statedata"]);
            }
        }

        public virtual string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return grainType.Split('.', '+').Last();
        }

        private void EnsureInitialized()
        {
            if (DataManager == null)
            {
                throw new ArgumentException("DataManager property not initialized");
            }
        }
    }
}