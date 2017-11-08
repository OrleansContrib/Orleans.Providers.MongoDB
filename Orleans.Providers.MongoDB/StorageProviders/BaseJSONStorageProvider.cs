using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public abstract class BaseJSONStorageProvider : IStorageProvider
    {
        private JsonSerializerSettings serializerSettings;
        private JsonSerializer serializer;
        private SerializationManager serializationManager;
        
        protected IJSONStateDataManager DataManager { get; set; }

        /// <summary>
        ///     Use JSON or Binary serialization
        /// </summary>
        public bool UseJsonFormat { get; private set; }

        /// <inheritoc />
        public string Name { get; private set; }

        /// <inheritoc />
        public Logger Log { get; private set; }

        /// <inheritoc />
        public virtual Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;

            Log = providerRuntime.GetLogger(GetType().FullName);

            serializationManager = providerRuntime.ServiceProvider.GetRequiredService<SerializationManager>();
            serializerSettings = 
                OrleansJsonSerializer.UpdateSerializerSettings(
                    OrleansJsonSerializer.GetDefaultSerializerSettings(serializationManager, providerRuntime.GrainFactory), config);
            serializerSettings.Converters.Add(new GrainReferenceConverter(providerRuntime.GrainFactory));
            serializer = JsonSerializer.Create(serializerSettings);

            UseJsonFormat = config.GetBoolProperty("UseJsonFormat", true);;

            return Task.CompletedTask;
        }

        /// <inheritoc />
        public Task Close()
        {
            DataManager?.Dispose();
            DataManager = null;

            return Task.CompletedTask;
        }

        /// <inheritoc />
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            EnsureInitialized();

            var grainTypeName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            var result = await DataManager.Read(grainTypeName, grainKey);

            if (result.Value != null)
            {
                ConvertFromStorageFormat(grainState, result.Value);

                grainState.ETag = result.Etag;
            }
        }

        /// <inheritoc />
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            EnsureInitialized();

            var grainTypeName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            var grainData = ConvertToStorageFormat(grainState);

            grainState.ETag = await DataManager.Write(grainTypeName, grainKey, grainData, grainState.ETag);
        }

        /// <inheritoc />
        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            EnsureInitialized();

            var grainTypeName = ReturnGrainName(grainType, grainReference);
            var grainKey = grainReference.ToKeyString();

            return DataManager.Delete(grainTypeName, grainKey);
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