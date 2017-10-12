using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text;
using Orleans.Providers.MongoDB.StorageProviders.Serializing;
using System.IO;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    ///     Base class for JSON-based grain storage providers.
    /// </summary>
    public abstract class BaseJSONStorageProvider : IStorageProvider
    {
        private JsonSerializerSettings serializerSettings;
        
        /// <summary>
        ///     Data manager instance
        /// </summary>
        /// <remarks>The data manager is responsible for reading and writing JSON strings.</remarks>
        protected IJSONStateDataManager DataManager { get; set; }

        private SerializationManager serializationManager;

        /// <summary>
        ///     Logger object
        /// </summary>
        public Logger Log { get; protected set; }

        /// <summary>
        ///     Storage provider name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        ///     Use JSON or Binary serialization
        /// </summary>
        public bool UseJsonFormat { get; set; }

        /// <summary>
        ///     Initializes the storage provider.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public virtual Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Log = providerRuntime.GetLogger(GetType().FullName);

            serializationManager = providerRuntime.ServiceProvider.GetRequiredService<SerializationManager>();
            serializerSettings = OrleansJsonSerializer.UpdateSerializerSettings(
                OrleansJsonSerializer.GetDefaultSerializerSettings(serializationManager, providerRuntime.GrainFactory),
                config);

            UseJsonFormat = config.GetBoolProperty("UseJsonFormat", true);
            serializerSettings.Converters.Add(new GrainReferenceConverter(providerRuntime.GrainFactory));
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Closes the storage provider during silo shutdown.
        /// </summary>
        /// <returns>Completion promise for this operation.</returns>
        public Task Close()
        {
            if (DataManager != null)
                DataManager.Dispose();
            DataManager = null;
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Reads persisted state from the backing store and deserializes it into the the target
        ///     grain state object.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">A reference to an object to hold the persisted state of the grain.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");

            var grainTypeName = ReturnGrainName(grainType, grainReference);

            var entityData = await DataManager.Read(grainTypeName, grainReference.ToKeyString());
            if (entityData != null)
                ConvertFromStorageFormat(grainState, entityData);
        }

        /// <summary>
        ///     Writes the persisted state from a grain state object into its backing store.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">A reference to an object holding the persisted state of the grain.</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");

            var grainTypeName = ReturnGrainName(grainType, grainReference);

            var entityData = ConvertToStorageFormat(grainState);
            return DataManager.Write(grainTypeName, grainReference.ToKeyString(), entityData);
        }

        /// <summary>
        ///     Removes grain state from its backing store, if found.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">An object holding the persisted state of the grain.</param>
        /// <returns></returns>
        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");

            var grainTypeName = ReturnGrainName(grainType, grainReference);

            DataManager.Delete(grainTypeName, grainReference.ToKeyString());
            return Task.CompletedTask;
        }


        public virtual string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return grainType.Split('.').Last();
        }

        /// <summary>
        ///     Serializes from a grain instance to a JSON document.
        /// </summary>
        /// <param name="grainState">Grain state to be converted into JSON storage format.</param>
        /// <remarks>
        ///     See:
        ///     http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer.aspx
        ///     for more on the JSON serializer.
        /// </remarks>
        protected BsonDocument ConvertToStorageFormat(IGrainState grainState)
        {
            if (UseJsonFormat)
            {
                var json = JsonConvert.SerializeObject(grainState.State, serializerSettings);
                json = ReverseInvalidValues(json);
                return BsonSerializer.Deserialize<BsonDocument>(json);
            }
            else
            {
                var bindata = serializationManager.SerializeToByteArray(grainState.State);
                return new BsonDocument
                {
                    ["statedata"] = bindata,
                };
            }
        }

        /// <summary>
        ///     Constructs a grain state instance by deserializing a JSON document.
        /// </summary>
        /// <param name="grainState">Grain state to be populated for storage.</param>
        /// <param name="entityData">JSON storage format representaiton of the grain state.</param>
        protected void ConvertFromStorageFormat(IGrainState grainState, BsonDocument entityData)
        {
            if(UseJsonFormat)
            {
                var json = entityData.ToJson();
                json = ReverseInvalidValues(json);
                JsonConvert.PopulateObject(json, grainState.State, serializerSettings);
            }
            else
            {
                grainState.State = serializationManager.DeserializeFromByteArray<object>((byte[])entityData["statedata"]);
            }
        }

        /// <summary>
        ///     NewtonSoft generates a $type & $id which is incompatible with Mongo. Replacing $ with __
        /// </summary>
        /// <param name="entityData"></param>
        /// <returns></returns>
        private string ReverseInvalidValues(string entityData)
        {
            var sb = new StringBuilder();
            var writer = new CustomJsonWriter(new StringWriter(sb));
            var reader = new JsonTextReader(new StringReader(entityData));

            while (reader.Read())
                if (reader.Value != null)
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.PropertyName:

                            if (reader.Value.ToString() == "$type")
                                writer.WritePropertyName("__type");
                            else if (reader.Value.ToString() == "__type")
                                writer.WritePropertyName("$type");
                            else if (reader.Value.ToString() == "$id")
                                writer.WritePropertyName("__id");
                            else if (reader.Value.ToString() == "__id")
                                writer.WritePropertyName("$id");
                            else if (reader.Value.ToString() == "$type")
                                writer.WritePropertyName("__type");
                            else if (reader.Value.ToString() == "__type")
                                writer.WritePropertyName("$type");
                            else if (reader.Value.ToString() == "$values")
                                writer.WritePropertyName("__values");
                            else if (reader.Value.ToString() == "__values")
                                writer.WritePropertyName("$values");
                            else
                                writer.WritePropertyName(reader.Value.ToString());

                            break;
                        case JsonToken.None:
                            break;
                        case JsonToken.StartConstructor:
                            writer.WriteStartConstructor(reader.Value.ToString());
                            break;
                        case JsonToken.Comment:
                            writer.WriteComment(reader.Value.ToString());
                            break;
                        case JsonToken.Raw:
                            writer.WriteRaw(reader.Value.ToString());
                            break;
                        case JsonToken.Integer:
                        case JsonToken.Float:
                        case JsonToken.String:
                        case JsonToken.Boolean:
                        case JsonToken.Date:
                        case JsonToken.Bytes:
                            writer.WriteValue(reader.Value);
                            break;
                        case JsonToken.Null:
                            writer.WriteNull();
                            break;
                        case JsonToken.Undefined:
                            writer.WriteUndefined();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.None:
                            break;
                        case JsonToken.StartObject:
                            writer.WriteStartObject();
                            break;
                        case JsonToken.StartArray:
                            writer.WriteStartArray();
                            break;
                        case JsonToken.Null:
                            writer.WriteNull();
                            break;
                        case JsonToken.Undefined:
                            writer.WriteUndefined();
                            break;
                        case JsonToken.EndObject:
                            writer.WriteEndObject();
                            break;
                        case JsonToken.EndArray:
                            writer.WriteEndArray();
                            break;
                        case JsonToken.EndConstructor:
                            writer.WriteEndConstructor();
                            break;
                        default:
                            break;
                    }
                }

            return sb.ToString();
        }
    }
}