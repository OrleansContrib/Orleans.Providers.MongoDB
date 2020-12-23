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
        private readonly Lazy<IMongoCollection<TEntity>> mongoCollection;
        private readonly bool createShardKey;

        protected IMongoCollection<TEntity> Collection
        {
            get { return mongoCollection.Value; }
        }

        protected IMongoDatabase Database
        {
            get { return mongoDatabase; }
        }

        public IMongoClient Client
        {
            get { return mongoClient; }
        }

        protected CollectionBase(IMongoClient mongoClient, string databaseName, bool createShardKey)
        {
            this.mongoClient = mongoClient;

            mongoDatabase = mongoClient.GetDatabase(databaseName);
            mongoCollection = CreateCollection();

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

        private Lazy<IMongoCollection<TEntity>> CreateCollection()
        {
            return new Lazy<IMongoCollection<TEntity>>(() =>
            {
                var collectionFilter = new ListCollectionNamesOptions
                {
                    Filter = Builders<BsonDocument>.Filter.Eq("name", CollectionName())
                };

                if (!mongoDatabase.ListCollectionNames(collectionFilter).Any())
                {
                    mongoDatabase.CreateCollection(CollectionName());
                }

                var databaseCollection = mongoDatabase.GetCollection<TEntity>(
                    CollectionName(),
                    CollectionSettings() ?? new MongoCollectionSettings());

                if (this.createShardKey)
                {
                    try
                    {
                        Database.RunCommand<BsonDocument>(new BsonDocument
                        {
                            ["key"] = new BsonDocument
                            {
                                ["_id"] = "hashed"
                            },
                            ["shardCollection"] = $"{mongoDatabase.DatabaseNamespace.DatabaseName}.{CollectionName()}"
                        });
                    }
                    catch (MongoException)
                    {
                        // Shared key probably created already.
                    }
                }

                SetupCollection(databaseCollection);

                return databaseCollection;
            });
        }
    }
}