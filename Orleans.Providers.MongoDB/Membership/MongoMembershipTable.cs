using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Membership.Store;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace Orleans.Providers.MongoDB.Membership
{
    public class MongoMembershipTable : IMembershipTable, IGatewayListProvider
    {
        private string deploymentId;
        private MongoMembershipCollection membershipCollection;
        private MongoMembershipCollection gatewaysCollection;
        private Logger logger;

        /// <inheritdoc />
        public bool IsUpdatable { get; } = true;

        /// <inheritdoc />
        public TimeSpan MaxStaleness { get; private set; }

        public Task InitializeGatewayListProvider(ClientConfiguration clientConfiguration, Logger traceLogger)
        {
            logger = traceLogger;

            deploymentId = clientConfiguration.DeploymentId;
            MaxStaleness = clientConfiguration.GatewayListRefreshPeriod;

            gatewaysCollection = 
                new MongoMembershipCollection(clientConfiguration.DataConnectionString, 
                    MongoUrl.Create(clientConfiguration.DataConnectionString).DatabaseName);

            return Task.CompletedTask;
        }

        public Task InitializeMembershipTable(GlobalConfiguration globalConfiguration, bool tryInitTableVersion, Logger traceLogger)
        {
            logger = traceLogger;

            deploymentId = globalConfiguration.DeploymentId;

            membershipCollection = 
                new MongoMembershipCollection(globalConfiguration.DataConnectionString, 
                    MongoUrl.Create(globalConfiguration.DataConnectionString).DatabaseName);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IList<Uri>> GetGateways()
        {
            return DoAndLog(nameof(GetGateways), () =>
            {
                return gatewaysCollection.GetGateways(deploymentId);
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
                return membershipCollection.ReadRow(deploymentId, key);
            });
        }

        /// <inheritdoc />
        public Task<MembershipTableData> ReadAll()
        {
            return DoAndLog(nameof(ReadAll), () =>
            {
                return membershipCollection.ReadAll(deploymentId);
            });
        }

        /// <inheritdoc />
        public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return DoAndLog(nameof(InsertRow), () =>
            {
                return membershipCollection.UpsertRow(deploymentId, entry, null);
            });
        }

        /// <inheritdoc />
        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return DoAndLog(nameof(UpdateRow), () =>
            {
                return membershipCollection.UpsertRow(deploymentId, entry, etag);
            });
        }

        /// <inheritdoc />
        public Task UpdateIAmAlive(MembershipEntry entry)
        {
            return DoAndLog(nameof(UpdateRow), () =>
            {
                return membershipCollection.UpdateIAmAlive(deploymentId, entry.SiloAddress, entry.IAmAliveTime);
            });
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            if (logger.IsVerbose3)
            {
                logger.Verbose3($"MongoMembershipTable.{actionName} called.");
            }

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                {
                    logger.Warn((int) MongoProviderErrorCode.MembershipTable_Operations, $"MongoMembershipTable.{actionName} failed. Exception={ex.Message}", ex);
                }

                throw;
            }
        }
    }
}