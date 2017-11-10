using System;
using System.Globalization;
using MongoDB.Driver;

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ArrangeAccessorOwnerBody

namespace Orleans.Providers.MongoDB.Utils
{
    public class CollectionBase<TEntity>
    {
        private const string CollectionFormat = "{0}Set";

        protected static readonly SortDefinitionBuilder<TEntity> Sort = Builders<TEntity>.Sort;
        protected static readonly UpdateDefinitionBuilder<TEntity> Update = Builders<TEntity>.Update;
        protected static readonly FilterDefinitionBuilder<TEntity> Filter = Builders<TEntity>.Filter;
        protected static readonly IndexKeysDefinitionBuilder<TEntity> Index = Builders<TEntity>.IndexKeys;
        protected static readonly ProjectionDefinitionBuilder<TEntity> Project = Builders<TEntity>.Projection;

        private readonly IMongoDatabase mongoDatabase;
        private readonly Lazy<IMongoCollection<TEntity>> mongoCollection;

        protected IMongoCollection<TEntity> Collection
        {
            get { return mongoCollection.Value; }
        }

        protected IMongoDatabase Database
        {
            get { return mongoDatabase; }
        }

        protected CollectionBase(string connectionString, string databaseName)
        {
            var client = MongoClientPool.Instance(connectionString);

            mongoDatabase = client.GetDatabase(databaseName);
            mongoCollection = CreateCollection();
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
                var databaseCollection = mongoDatabase.GetCollection<TEntity>(
                    CollectionName(),
                    CollectionSettings() ?? new MongoCollectionSettings());

                SetupCollection(databaseCollection);

                return databaseCollection;
            });
        }
    }
}