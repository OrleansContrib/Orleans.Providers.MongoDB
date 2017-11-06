using System;
using System.Globalization;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Orleans.Providers.MongoDB.Repository
{
    public class DocumentRepository2<TEntity>
    {
        private const string CollectionFormat = "{0}Set";

        protected static readonly SortDefinitionBuilder<TEntity> Sort = Builders<TEntity>.Sort;
        protected static readonly UpdateDefinitionBuilder<TEntity> Update = Builders<TEntity>.Update;
        protected static readonly FilterDefinitionBuilder<TEntity> Filter = Builders<TEntity>.Filter;
        protected static readonly IndexKeysDefinitionBuilder<TEntity> Index = Builders<TEntity>.IndexKeys;
        protected static readonly ProjectionDefinitionBuilder<TEntity> Project = Builders<TEntity>.Projection;

        private readonly IMongoDatabase mongoDatabase;
        private Lazy<IMongoCollection<TEntity>> mongoCollection;

        protected IMongoCollection<TEntity> Collection
        {
            get { return mongoCollection.Value; }
        }

        protected IMongoDatabase Database
        {
            get { return mongoDatabase; }
        }

        protected DocumentRepository2(string connectionString, string databaseName)
        {
            var client = MongoClientManager.Instance(connectionString);

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

        protected virtual Task SetupCollectionAsync(IMongoCollection<TEntity> collection)
        {
            return Task.CompletedTask;
        }

        private Lazy<IMongoCollection<TEntity>> CreateCollection()
        {
            return new Lazy<IMongoCollection<TEntity>>(() =>
            {
                var databaseCollection = mongoDatabase.GetCollection<TEntity>(
                    CollectionName(),
                    CollectionSettings() ?? new MongoCollectionSettings());

                SetupCollectionAsync(databaseCollection).Wait();

                return databaseCollection;
            });
        }
    }
}