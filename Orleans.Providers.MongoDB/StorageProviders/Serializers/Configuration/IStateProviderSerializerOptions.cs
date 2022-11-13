namespace Orleans.Providers.MongoDB.StorageProviders.Serializers.Configuration
{
    public interface IStateProviderSerializerOptions
    {
        /// <summary>
        /// Gets or sets the serializer to use for this storage provider.
        /// </summary>
        IGrainStateSerializer GrainStateSerializer { get; set; }
    }
}
