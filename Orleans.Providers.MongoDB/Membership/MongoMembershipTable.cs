using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Membership.Store;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

// ReSharper disable ConvertToLambdaExpression

namespace Orleans.Providers.MongoDB.Membership
{
    public class MongoMembershipTable : IMembershipTable
    {
        private readonly ILogger<MongoMembershipTable> logger;
        private readonly GlobalConfiguration config;
        private MongoMembershipCollection membershipCollection;
        
        public MongoMembershipTable(ILogger<MongoMembershipTable> logger, GlobalConfiguration config)
        {
            this.logger = logger;
            this.config = config;
        }

        /// <inheritdoc />
        public Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            return DoAndLog(nameof(InitializeMembershipTable), () =>
            {
                membershipCollection =
                    new MongoMembershipCollection(config.DataConnectionString,
                        MongoUrl.Create(config.DataConnectionString).DatabaseName);

                return Task.CompletedTask;
            });
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
                return membershipCollection.ReadRow(config.DeploymentId, key);
            });
        }

        /// <inheritdoc />
        public Task<MembershipTableData> ReadAll()
        {
            return DoAndLog(nameof(ReadAll), () =>
            {
                return membershipCollection.ReadAll(config.DeploymentId);
            });
        }

        /// <inheritdoc />
        public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return DoAndLog(nameof(InsertRow), () =>
            {
                return membershipCollection.UpsertRow(config.DeploymentId, entry, null);
            });
        }

        /// <inheritdoc />
        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return DoAndLog(nameof(UpdateRow), () =>
            {
                return membershipCollection.UpsertRow(config.DeploymentId, entry, etag);
            });
        }

        /// <inheritdoc />
        public Task UpdateIAmAlive(MembershipEntry entry)
        {
            return DoAndLog(nameof(UpdateRow), () =>
            {
                return membershipCollection.UpdateIAmAlive(config.DeploymentId, entry.SiloAddress, entry.IAmAliveTime);
            });
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            logger.LogInformation($"{nameof(MongoMembershipTable)}.{actionName} called.");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogError((int) MongoProviderErrorCode.MembershipTable_Operations, $"{nameof(MongoMembershipTable)}.{actionName} failed. Exception={ex.Message}", ex);

                throw;
            }
        }
    }
}