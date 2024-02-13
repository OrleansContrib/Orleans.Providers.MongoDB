using System;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ArrangeAccessorOwnerBody

namespace Orleans.Providers.MongoDB.Utils
{
    public class CollectionBase<TEntity>
    {
        private const string CollectionFormat = "{0}Set";

        protected static readonly UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };
        protected static readonly ReplaceOptions UpsertReplace = new ReplaceOptions { IsUpsert = true };
        protected static readonly SortDefinitionBuilder<TEntity> Sort = Builders<TEntity>.Sort;
        protected static readonly UpdateDefinitionBuilder<TEntity> Update = Builders<TEntity>.Update;
        protected static readonly FilterDefinitionBuilder<TEntity> Filter = Builders<TEntity>.Filter;
        protected static readonly IndexKeysDefinitionBuilder<TEntity> Index = Builders<TEntity>.IndexKeys;
        protected static readonly ProjectionDefinitionBuilder<TEntity> Project = Builders<TEntity>.Projection;

        private readonly IMongoDatabase mongoDatabase;
        private readonly IMongoClient mongoClient;
        private readonly Action<MongoCollectionSettings> collectionConfigurator;
        private IMongoCollection<TEntity> mongoCollection;
        private readonly object mongoCollectionInitializerLock = new();
        private readonly bool createShardKey;

        protected IMongoCollection<TEntity> Collection
        {
            get
            {
                if (mongoCollection == null)
                {
                    lock (mongoCollectionInitializerLock)
                    {
                        mongoCollection ??= CreateCollection(collectionConfigurator);
                    }
                }

                return mongoCollection;
            }
        }

        protected IMongoDatabase Database
        {
            get { return mongoDatabase; }
        }

        public IMongoClient Client
        {
            get { return mongoClient; }
        }

        protected CollectionBase(IMongoClient mongoClient, string databaseName,
            Action<MongoCollectionSettings> collectionConfigurator, bool createShardKey)
        {
            this.mongoClient = mongoClient;
            this.collectionConfigurator = collectionConfigurator;

            mongoDatabase = mongoClient.GetDatabase(databaseName);

            this.createShardKey = createShardKey;
        }

        protected virtual MongoCollectionSettings CollectionSettings()
        {
            return new MongoCollectionSettings();
        }

        protected virtual string CollectionName()
        {
            return string.Format(CultureInfo.InvariantCulture, CollectionFormat, typeof(TEntity).Name);
        }

        protected virtual void SetupCollection(IMongoCollection<TEntity> collection)
        {
        }

        private IMongoCollection<TEntity> CreateCollection(Action<MongoCollectionSettings> collectionConfigurator)
        {
            var collectionName = CollectionName();

            var collectionSettings = CollectionSettings() ?? new MongoCollectionSettings();

            collectionConfigurator?.Invoke(collectionSettings);

            var databaseCollection = mongoDatabase.GetCollection<TEntity>(
                collectionName,
                collectionSettings);

            if (createShardKey)
            {
                try
                {
                    mongoClient.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument
                    {
                        ["shardCollection"] = $"{mongoDatabase.DatabaseNamespace.DatabaseName}.{collectionName}",
                        ["key"] = new BsonDocument
                        {
                            ["_id"] = "hashed"
                        }
                    });
                }
                catch (MongoException)
                {
                    // Shared key probably created already.
                }
            }

            SetupCollection(databaseCollection);

            return databaseCollection;
        }
    }
}