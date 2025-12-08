using System;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;

namespace Orleans.Providers.MongoDB.Reminders.Store;

internal static class Factory
{
    public static IMongoReminderCollection Create(IMongoClient mongoClient, MongoDBRemindersOptions  options, string serviceId)
    {
        return options.Strategy switch
        {
            MongoDBReminderStrategy.DefaultStorage =>
                new MongoReminderCollection(
                    mongoClient,
                    options.DatabaseName,
                    options.CollectionPrefix,
                    options.CollectionConfigurator,
                    options.CreateShardKeyForCosmos,
                    serviceId
                ),
            MongoDBReminderStrategy.HashedLookupStorage =>
                new MongoReminderHashedCollection(
                    mongoClient,
                    options.DatabaseName,
                    options.CollectionPrefix,
                    options.CollectionConfigurator,
                    options.CreateShardKeyForCosmos,
                    serviceId
                ),
            _ => throw new ArgumentException("Invalid strategy.", nameof(options.Strategy))
        };
    }
}