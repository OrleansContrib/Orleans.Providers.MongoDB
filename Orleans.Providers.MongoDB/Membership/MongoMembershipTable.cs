﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Membership.Store;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Configuration;

// ReSharper disable ConvertToLambdaExpression

namespace Orleans.Providers.MongoDB.Membership
{
    public sealed class MongoMembershipTable : IMembershipTable
    {
        private readonly ILogger<MongoMembershipTable> logger;
        private readonly MongoDBMembershipTableOptions options;
        private readonly string clusterId;
        private MongoMembershipCollection membershipCollection;
        
        public MongoMembershipTable(
            ILogger<MongoMembershipTable> logger,
            IOptions<ClusterOptions> clusterOptions,
            IOptions<MongoDBMembershipTableOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;
            this.clusterId = clusterOptions.Value.ClusterId;
        }

        /// <inheritdoc />
        public Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            membershipCollection =
                new MongoMembershipCollection(
                    options.ConnectionString,
                    options.DatabaseName,
                    options.CollectionPrefix);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteMembershipTableEntries(string deploymentId)
        {
            return DoAndLog(nameof(DeleteMembershipTableEntries), () =>
            {
                return membershipCollection.DeleteMembershipTableEntries(deploymentId);
            });
        }

        /// <inheritdoc />
        public Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            return DoAndLog(nameof(ReadRow), () =>
            {
                return membershipCollection.ReadRow(clusterId, key);
            });
        }

        /// <inheritdoc />
        public Task<MembershipTableData> ReadAll()
        {
            return DoAndLog(nameof(ReadAll), () =>
            {
                return membershipCollection.ReadAll(clusterId);
            });
        }

        /// <inheritdoc />
        public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return DoAndLog(nameof(InsertRow), () =>
            {
                return membershipCollection.UpsertRow(clusterId, entry, null);
            });
        }

        /// <inheritdoc />
        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return DoAndLog(nameof(UpdateRow), () =>
            {
                return membershipCollection.UpsertRow(clusterId, entry, etag);
            });
        }

        /// <inheritdoc />
        public Task UpdateIAmAlive(MembershipEntry entry)
        {
            return DoAndLog(nameof(UpdateRow), () =>
            {
                return membershipCollection.UpdateIAmAlive(clusterId, 
                    entry.SiloAddress,
                    entry.IAmAliveTime);
            });
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
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