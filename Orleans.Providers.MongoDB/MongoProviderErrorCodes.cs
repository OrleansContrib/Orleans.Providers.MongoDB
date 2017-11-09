namespace Orleans.Providers.MongoDB
{
    internal enum MongoProviderErrorCode
    {
        ProvidersBase = 900000,
        
        StorageProviderBase = ProvidersBase + 100,
        StorageProvider_Reading = StorageProviderBase + 4,
        StorageProvider_Writing = StorageProviderBase + 5,
        StorageProvider_Deleting = StorageProviderBase + 6,

        MembershipTable_Operations = ProvidersBase + 200,

        StatisticsPublisher_Operations = ProvidersBase + 300,

        Reminders_Operations = ProvidersBase + 400
    }
}
