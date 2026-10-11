using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Membership.Store;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Configuration;
using MongoDB.Driver;

// ReSharper disable ConvertToLambdaExpression

namespace Orleans.Providers.MongoDB.Membership
{
    public sealed class MongoMembershipTable : IMembershipTable
    {
        private readonly IMongoClient mongoClient;
        private readonly ILogger<MongoMembershipTable> logger;
        private readonly MongoDBMembershipTableOptions options;
        private readonly string clusterId;
        private IMongoMembershipCollection membershipCollection;
        
        public MongoMembershipTable(
            IMongoClientFactory mongoClientFactory,
            ILogger<MongoMembershipTable> logger,
            IOptions<ClusterOptions> clusterOptions,
            IOptions<MongoDBMembershipTableOptions> options)
        {
            this.mongoClient = mongoClientFactory.Create(options.Value, "Membership");
            this.logger = logger;
            this.options = options.Value;
            this.clusterId = clusterOptions.Value.ClusterId;
        }

        /// <inheritdoc />
        [Obsolete("Use InitializeMembershipTableAsync instead.")]
        public Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            return InitializeMembershipTableAsync(tryInitTableVersion, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task InitializeMembershipTableAsync(bool tryInitTableVersion, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            membershipCollection = Factory.CreateCollection(mongoClient, options, options.Strategy);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        [Obsolete("Use DeleteMembershipTableEntriesAsync instead.")]
        public Task DeleteMembershipTableEntries(string deploymentId)
        {
            return DeleteMembershipTableEntriesAsync(deploymentId, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task DeleteMembershipTableEntriesAsync(string deploymentId, CancellationToken cancellationToken = default)
        {
            return DoAndLog(nameof(DeleteMembershipTableEntriesAsync), () =>
            {
                return membershipCollection.DeleteMembershipTableEntries(deploymentId, cancellationToken);
            });
        }

        /// <inheritdoc />
        [Obsolete("Use ReadRowAsync instead.")]
        public Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            return ReadRowAsync(key, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<MembershipTableData> ReadRowAsync(SiloAddress key, CancellationToken cancellationToken = default)
        {
            return DoAndLog(nameof(ReadRowAsync), () =>
            {
                return membershipCollection.ReadRow(clusterId, key, cancellationToken);
            });
        }

        /// <inheritdoc />
        [Obsolete("Use ReadAllAsync instead.")]
        public Task<MembershipTableData> ReadAll()
        {
            return ReadAllAsync(CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<MembershipTableData> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return DoAndLog(nameof(ReadAllAsync), () =>
            {
                return membershipCollection.ReadAll(clusterId, cancellationToken);
            });
        }

        /// <inheritdoc />
        [Obsolete("Use InsertRowAsync instead.")]
        public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return InsertRowAsync(entry, tableVersion, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<bool> InsertRowAsync(MembershipEntry entry, TableVersion tableVersion, CancellationToken cancellationToken = default)
        {
            return DoAndLog(nameof(InsertRowAsync), () =>
            {
                return membershipCollection.UpsertRow(clusterId, entry, null, tableVersion, cancellationToken);
            });
        }

        /// <inheritdoc />
        [Obsolete("Use UpdateRowAsync instead.")]
        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return UpdateRowAsync(entry, etag, tableVersion, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<bool> UpdateRowAsync(MembershipEntry entry, string etag, TableVersion tableVersion, CancellationToken cancellationToken = default)
        {
            return DoAndLog(nameof(UpdateRowAsync), () =>
            {
                return membershipCollection.UpsertRow(clusterId, entry, etag, tableVersion, cancellationToken);
            });
        }

        /// <inheritdoc />
        [Obsolete("Use CleanupDefunctSiloEntriesAsync instead.")]
        public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
        {
            return CleanupDefunctSiloEntriesAsync(beforeDate, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task CleanupDefunctSiloEntriesAsync(DateTimeOffset beforeDate, CancellationToken cancellationToken = default)
        {
            return DoAndLog(nameof(CleanupDefunctSiloEntriesAsync), () =>
            {
                return membershipCollection.CleanupDefunctSiloEntries(clusterId, beforeDate, cancellationToken);
            });
        }

        /// <inheritdoc />
        [Obsolete("Use UpdateIAmAliveAsync instead.")]
        public Task UpdateIAmAlive(MembershipEntry entry)
        {
            return UpdateIAmAliveAsync(entry, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task UpdateIAmAliveAsync(MembershipEntry entry, CancellationToken cancellationToken = default)
        {
            return DoAndLog(nameof(UpdateIAmAliveAsync), () =>
            {
                return membershipCollection.UpdateIAmAlive(clusterId,
                    entry.SiloAddress,
                    entry.IAmAliveTime,
                    cancellationToken);
            });
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () =>
            {
                await action();

                return true;
            });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            logger.LogDebug($"{nameof(MongoMembershipTable)}.{actionName} called.");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogError((int) MongoProviderErrorCode.MembershipTable_Operations, ex, $"{nameof(MongoMembershipTable)}.{actionName} failed. Exception={ex.Message}");

                throw;
            }
        }
    }
}