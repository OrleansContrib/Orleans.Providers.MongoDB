using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Membership.Repository;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace Orleans.Providers.MongoDB.Membership
{
    /// <summary>
    ///     The mongo membership provider. It is used to manage cluster members as well as provide gateway
    ///     servers
    /// </summary>
    public class MongoMembershipTable : IMembershipTable, IGatewayListProvider
    {
        private string deploymentId;

        private IGatewayProviderRepository gatewayRepository;
        private Logger logger;
        private IMongoMembershipRepository membershipRepository;

        /// <summary>
        ///     The initialize gateway list provider.
        /// </summary>
        /// <param name="clientConfiguration">
        ///     The client configuration.
        /// </param>
        /// <param name="traceLogger">
        ///     The trace logger.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public Task InitializeGatewayListProvider(ClientConfiguration clientConfiguration, Logger traceLogger)
        {
            logger = traceLogger;
            if (logger.IsVerbose3)
                logger.Verbose3("MongoMembershipTable.InitializeGatewayListProvider called.");

            deploymentId = clientConfiguration.DeploymentId;
            MaxStaleness = clientConfiguration.GatewayListRefreshPeriod;

            gatewayRepository = new GatewayProviderRepository(clientConfiguration.DataConnectionString,
                MongoUrl.Create(clientConfiguration.DataConnectionString).DatabaseName);

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Returns the active gateways.
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task<IList<Uri>> GetGateways()
        {
            try
            {
                if (logger.IsVerbose3)
                    logger.Verbose3("MongoMembershipTable.GetGateways called.");

                return await gatewayRepository.ReturnActiveGatewaysAsync(deploymentId);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("MongoMembershipTable.Gateways failed {0}", ex);

                throw;
            }
        }

        public bool IsUpdatable => true;

        public TimeSpan MaxStaleness { get; private set; }

        public async Task InitializeMembershipTable(
            GlobalConfiguration globalConfiguration,
            bool tryInitTableVersion,
            Logger traceLogger)
        {
            logger = traceLogger;
            deploymentId = globalConfiguration.DeploymentId;

            if (logger.IsVerbose3)
                logger.Verbose3("MongoMembershipTable.InitializeMembershipTable called.");

            membershipRepository = new MongoMembershipRepository(globalConfiguration.DataConnectionString,
                MongoUrl.Create(globalConfiguration.DataConnectionString).DatabaseName);

            // even if I am not the one who created the table, 
            // try to insert an initial table version if it is not already there,
            // so we always have a first table version row, before this silo starts working.
            if (tryInitTableVersion)
            {
                var wasCreated = await InitTableAsync();
                if (wasCreated)
                    logger.Info("Created new table version row.");
            }
        }

        public async Task DeleteMembershipTableEntries(string deploymentId)
        {
            try
            {
                if (logger.IsVerbose3)
                    logger.Verbose3(
                        $"MongoMembershipTable.DeleteMembershipTableEntries called with deploymentId {deploymentId}.");

                await membershipRepository.DeleteMembershipTableEntriesAsync(deploymentId);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("MongoMembershipTable.DeleteMembershipTableEntries failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        ///     Returns a membership corresponding to a SiloAddress for a deployment. If the corresponding membership version
        ///     doesn't contain a version number,
        ///     it is set to 1.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            try
            {
                if (logger.IsVerbose3)
                    logger.Verbose3("MongoMembershipTable.ReadRow called.");

                return await membershipRepository.ReturnRow(key, deploymentId);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("MongoMembershipTable.ReadRow failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        ///     Returns all the memberships for a deploymentId
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task<MembershipTableData> ReadAll()
        {
            try
            {
                if (logger.IsVerbose3)
                    logger.Verbose3("MongoMembershipTable.ReadAll called.");

                return await membershipRepository.ReturnAllRows(deploymentId);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("MongoMembershipTable.ReadAll failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        ///     Insert a membership as well as version if one does not exist
        /// </summary>
        /// <param name="entry">
        ///     The entry.
        /// </param>
        /// <param name="tableVersion">
        ///     The table version.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            if (logger.IsVerbose3)
                logger.Verbose3(
                    string.Format(
                        "MongoMembershipTable.InsertRow called with entry {0} and tableVersion {1}.",
                        entry,
                        tableVersion));

            // The "tableVersion" parameter should always exist when inserting a row as Init should
            // have been called and membership version created and read. This is an optimization to
            // not to go through all the way to database to fail a conditional check on etag (which does
            // exist for the sake of robustness) as mandated by Orleans membership protocol.
            // Likewise, no update can be done without membership entry.
            if (entry == null)
            {
                if (logger.IsVerbose)
                    logger.Verbose(
                        "MongoMembershipTable.InsertRow aborted due to null check. MembershipEntry is null.");

                throw new ArgumentNullException("entry");
            }

            if (tableVersion == null)
            {
                if (logger.IsVerbose)
                    logger.Verbose(
                        "MongoMembershipTable.InsertRow aborted due to null check. TableVersion is null ");

                throw new ArgumentNullException("tableVersion");
            }

            try
            {
                return await membershipRepository.InsertMembershipRow(deploymentId, entry, tableVersion);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("MongoMembershipTable.InsertRow failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        ///     Update a Membership.
        /// </summary>
        /// <param name="entry">
        ///     The entry.
        /// </param>
        /// <param name="etag">
        ///     The etag.
        /// </param>
        /// <param name="tableVersion">
        ///     The table version.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            if (logger.IsVerbose3)
                logger.Verbose3(
                    string.Format(
                        "MongoMembershipTable.UpdateRow called with entry {0}, etag {1} and tableVersion {2}.",
                        entry,
                        etag,
                        tableVersion));

            // The "tableVersion" parameter should always exist when updating a row as Init should
            // have been called and membership version created and read. This is an optimization to
            // not to go through all the way to database to fail a conditional check (which does
            // exist for the sake of robustness) as mandated by Orleans membership protocol.
            // Likewise, no update can be done without membership entry or an etag.
            if (entry == null)
            {
                if (logger.IsVerbose)
                    logger.Verbose(
                        "MongoMembershipTable.UpdateRow aborted due to null check. MembershipEntry is null.");

                throw new ArgumentNullException("entry");
            }

            if (tableVersion == null)
            {
                if (logger.IsVerbose)
                    logger.Verbose(
                        "MongoMembershipTable.UpdateRow aborted due to null check. TableVersion is null ");

                throw new ArgumentNullException("tableVersion");
            }

            try
            {
                return await membershipRepository.UpdateMembershipRowAsync(deploymentId, entry,
                    tableVersion.VersionEtag);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("MongoMembershipTable.UpdateRow failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        ///     Update I am alive.
        /// </summary>
        /// <param name="entry">
        ///     The entry.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            if (logger.IsVerbose3)
                logger.Verbose3($"MongoMembershipTable.UpdateIAmAlive called with entry {entry}.");

            if (entry == null)
            {
                if (logger.IsVerbose)
                    logger.Verbose(
                        "MongoMembershipTable.UpdateIAmAlive aborted due to null check. MembershipEntry is null.");

                throw new ArgumentNullException("entry");
            }

            try
            {
                await membershipRepository.UpdateIAmAliveTimeAsyncTask(
                    deploymentId,
                    entry.SiloAddress,
                    entry.IAmAliveTime);
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose)
                    logger.Verbose("MongoMembershipTable.UpdateIAmAlive failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        ///     Init Membership.
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        private async Task<bool> InitTableAsync()
        {
            try
            {
                await membershipRepository.InitMembershipVersionCollectionAsync(deploymentId);
                return true;
            }
            catch (Exception ex)
            {
                if (logger.IsVerbose2)
                    logger.Verbose2("Insert silo membership version failed: {0}", ex.ToString());

                throw;
            }
        }
    }
}