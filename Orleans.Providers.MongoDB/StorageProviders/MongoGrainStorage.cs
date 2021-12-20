﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    public class MongoGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private static readonly UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };
        private static readonly ReplaceOptions UpsertReplace = new ReplaceOptions { IsUpsert = true };
        private static readonly FilterDefinitionBuilder<BsonDocument> Filter = Builders<BsonDocument>.Filter;
        private static readonly UpdateDefinitionBuilder<BsonDocument> Update = Builders<BsonDocument>.Update;
        private static readonly ProjectionDefinitionBuilder<BsonDocument> Projection = Builders<BsonDocument>.Projection;
        private const string FieldId = "_id";
        private const string FieldDoc = "_doc";
        private const string FieldEtag = "_etag";
        private readonly ConcurrentDictionary<string, bool> configuredCollections = new ConcurrentDictionary<string, bool>();
        private readonly MongoDBGrainStorageOptions options;
        private readonly IMongoClient mongoClient;
        private readonly ILogger<MongoGrainStorage> logger;
        private readonly IGrainStateSerializer serializer;
        private IMongoDatabase database;

        public MongoGrainStorage(
            IMongoClientFactory mongoClientFactory,
            ILogger<MongoGrainStorage> logger,
            IGrainStateSerializer serializer,
            MongoDBGrainStorageOptions options)
        {
            this.mongoClient = mongoClientFactory.Create(options, "Storage");
            this.logger = logger;
            this.options = options;
            this.serializer = serializer;
        }

        protected virtual JsonSerializerSettings ReturnSerializerSettings(ITypeResolver typeResolver, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            return OrleansJsonSerializer.UpdateSerializerSettings(OrleansJsonSerializer.GetDefaultSerializerSettings(typeResolver, providerRuntime.GrainFactory), config);
        }

        protected virtual string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return grainType.Split('.', '+').Last();
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe<MongoGrainStorage>(ServiceLifecycleStage.ApplicationServices, Init);
        }

        private Task Init(CancellationToken ct)
        {
            return DoAndLog(nameof(Init), () =>
            {
                database = mongoClient.GetDatabase(options.DatabaseName);

                return Task.CompletedTask;
            });
        }

        public Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            return DoAndLog(nameof(ReadStateAsync), async () =>
            {
                var grainCollection = GetCollection(grainType);
                var grainKey = this.options.KeyGenerator(grainReference);

                var existing =
                    await grainCollection.Find(Filter.Eq(FieldId, grainKey))
                        .FirstOrDefaultAsync();

                if (existing != null)
                {
                    grainState.RecordExists = true;

                    if (existing.Contains(FieldDoc))
                    {
                        grainState.ETag = existing[FieldEtag].AsString;

                        serializer.Deserialize(grainState, existing[FieldDoc].AsBsonDocument.ToJToken());
                    }
                    else
                    {
                        existing.Remove(FieldId);

                        serializer.Deserialize(grainState, existing.ToJToken());
                    }
                }
            });
        }

        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            return DoAndLog(nameof(WriteStateAsync), async () =>
            {
                var grainCollection = GetCollection(grainType);
                var grainKey = options.KeyGenerator(grainReference);

                var grainData = serializer.Serialize(grainState);

                var etag = grainState.ETag;

                var newData = grainData.ToBson();
                var newETag = Guid.NewGuid().ToString();

                grainState.RecordExists = true;

                try
                {
                    await grainCollection.UpdateOneAsync(
                        Filter.And(
                            Filter.Eq(FieldId, grainKey),
                            Filter.Eq(FieldEtag, grainState.ETag)
                        ),
                        Update
                            .Set(FieldEtag, newETag)
                            .Set(FieldDoc, newData),
                        Upsert);
                }
                catch (MongoException ex)
                {
                    if (ex.IsDuplicateKey())
                    {
                        await ThrowForOtherEtag(grainCollection, grainKey, etag, ex);

                        var document = new BsonDocument
                        {
                            [FieldId] = grainKey,
                            [FieldEtag] = grainKey,
                            [FieldDoc] = newData
                        };

                        try
                        {
                            await grainCollection.ReplaceOneAsync(Filter.Eq(FieldId, grainKey), document, UpsertReplace);
                        }
                        catch (MongoException ex2)
                        {
                            if (ex2.IsDuplicateKey())
                            {
                                await ThrowForOtherEtag(grainCollection, grainKey, etag, ex2);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                grainState.ETag = newETag;
            });
        }

        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            return DoAndLog(nameof(ClearStateAsync), () =>
            {
                var grainCollection = GetCollection(grainType);
                var grainKey = options.KeyGenerator(grainReference);

                grainState.RecordExists = false;

                return grainCollection.DeleteManyAsync(Filter.Eq(FieldId, grainKey));
            });
        }

        private IMongoCollection<BsonDocument> GetCollection(string grainType)
        {
            var collectionName = options.CollectionPrefix + grainType.Split('.', '+').Last();

            if (configuredCollections.TryAdd(collectionName, true))
            {
                if (options.CreateShardKeyForCosmos)
                {
                    try
                    {
                        database.RunCommand<BsonDocument>(new BsonDocument
                        {
                            ["key"] = new BsonDocument
                            {
                                ["_id"] = "hashed"
                            },
                            ["shardCollection"] = $"{database.DatabaseNamespace.DatabaseName}.{collectionName}"
                        });
                    }
                    catch (MongoException)
                    {
                        // Shared key probably created already.
                    }
                }
            }

            return database.GetCollection<BsonDocument>(collectionName);
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
        }

        private static async Task ThrowForOtherEtag(IMongoCollection<BsonDocument> collection, string key, string etag, Exception ex)
        {
            var existingEtag =
                await collection.Find(Filter.Eq(FieldId, key))
                    .Project<BsonDocument>(Projection.Exclude(FieldDoc)).FirstOrDefaultAsync();

            if (existingEtag != null && existingEtag.Contains(FieldEtag))
            {
                throw new InconsistentStateException(existingEtag[FieldEtag].AsString, etag, ex);
            }
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            logger.LogDebug($"{nameof(MongoGrainStorage)}.{actionName} called.");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogError((int)MongoProviderErrorCode.GrainStorageOperations, ex, $"{nameof(MongoGrainStorage)}.{actionName} failed. Exception={ex.Message}");

                throw;
            }
        }
    }
}
