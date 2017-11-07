using System;
using System.Reflection;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;

public class GrainReferenceConverter : JsonConverter
{
    private static readonly Type AddressableType = typeof(IAddressable);
    private readonly IGrainFactory grainFactory;
    private readonly JsonSerializer internalSerializer;

    public GrainReferenceConverter(IGrainFactory grainFactory)
    {
        this.grainFactory = grainFactory;

        // Create a serializer for internal serialization which does not have a specified GrainReference serializer.
        // This internal serializer will use GrainReference's ISerializable implementation for serialization and deserialization.
        this.internalSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            Formatting = Formatting.None,
            //Converters =
            //    {
            //        new IPAddressConverter(),
            //        new IPEndPointConverter(),
            //        new GrainIdConverter(),
            //        new SiloAddressConverter(),
            //        new UniqueKeyConverter()
            //    }
        });
    }

    public override bool CanConvert(Type objectType)
    {
        return AddressableType.IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // Serialize the grain reference using the internal serializer.
        this.internalSerializer.Serialize(writer, value);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Deserialize using the internal serializer which will use the concrete GrainReference implementation's
        // ISerializable constructor.
        var result = this.internalSerializer.Deserialize(reader, objectType);
        var grainRef = result as IAddressable;
        if (grainRef == null) return result;

        // Bind the deserialized grain reference to the runtime.
        this.grainFactory.BindGrainReference(grainRef);
        return grainRef;
    }
}