namespace Orleans.Providers.MongoDB.Membership
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    using Orleans.Messaging;
    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;
    using Orleans.Runtime.Configuration;

    /// <summary>
    /// The mongo membership provider.
    /// </summary>
    public class MongoMembershipProvider : IMembershipTable, IGatewayListProvider
    {
        private string deploymentId;
        private TimeSpan maxStaleness;
        private Logger logger;
        private IMongoMembershipProviderRepository repository;

        #region Implementation of IMembershipTable

        /// <summary>
        /// Initializes the membership table, will be called before all other methods
        /// </summary>
        /// <param name="globalConfiguration">the give global configuration</param>
        /// <param name="tryInitTableVersion">whether an attempt will be made to init the underlying table</param>
        /// <param name="traceLogger">the logger used by the membership table</param>
        public async Task InitializeMembershipTable(
            GlobalConfiguration globalConfiguration,
            bool tryInitTableVersion,
            TraceLogger traceLogger)
        {
            this.logger = traceLogger;
            this.deploymentId = globalConfiguration.DeploymentId;

            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3("MongoMembershipTable.InitializeMembershipTable called.");
            }
            
            this.repository = new MongoMembershipProviderRepository(globalConfiguration.DataConnectionString, MongoUrl.Create(globalConfiguration.DataConnectionString).DatabaseName);

            // even if I am not the one who created the table, 
            // try to insert an initial table version if it is not already there,
            // so we always have a first table version row, before this silo starts working.
            if (tryInitTableVersion)
            {
                var wasCreated = await this.InitTableAsync();
                if (wasCreated)
                {
                    this.logger.Info("Created new table version row.");
                }
            }
        }

        /// <summary>
        /// Deletes all table entries of the given deploymentId
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment Id.
        /// </param>
        public Task DeleteMembershipTableEntries(string deploymentId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Atomically reads the Membership Table information about a given silo.
        /// The returned MembershipTableData includes one MembershipEntry entry for a given silo and the
        /// TableVersion for this table. The MembershipEntry and the TableVersion have to be read atomically.
        /// </summary>
        /// <param name="entry">The address of the silo whose membership information needs to be read.</param>
        /// <returns>The membership information for a given silo: MembershipTableData consisting one MembershipEntry entry and
        /// TableVersion, read atomically.</returns>
        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            return await this.repository.ReturnRow(key, this.deploymentId);
        }

        /// <summary>
        /// Atomically reads the full content of the Membership Table.
        /// The returned MembershipTableData includes all MembershipEntry entry for all silos in the table and the
        /// TableVersion for this table. The MembershipEntries and the TableVersion have to be read atomically.
        /// </summary>
        /// <returns>The membership information for a given table: MembershipTableData consisting multiple MembershipEntry entries and
        /// TableVersion, all read atomically.</returns>
        public async Task<MembershipTableData> ReadAll()
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3("MongoMembershipTable.ReadAll called.");
            }

            try
            {
                //Todo: Update Suspecting Silos
                return await this.repository.ReturnMembershipTableData(this.deploymentId);
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("MongoMembershipTable.ReadAll failed: {0}", ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Atomically tries to insert (add) a new MembershipEntry for one silo and also update the TableVersion.
        /// If operation succeeds, the following changes would be made to the table:
        /// 1) New MembershipEntry will be added to the table.
        /// 2) The newly added MembershipEntry will also be added with the new unique automatically generated eTag.
        /// 3) TableVersion.Version in the table will be updated to the new TableVersion.Version.
        /// 4) TableVersion etag in the table will be updated to the new unique automatically generated eTag.
        /// All those changes to the table, insert of a new row and update of the table version and the associated etags, should happen atomically, or fail atomically with no side effects.
        /// The operation should fail in each of the following conditions:
        /// 1) A MembershipEntry for a given silo already exist in the table
        /// 2) Update of the TableVersion failed since the given TableVersion etag (as specified by the TableVersion.VersionEtag property) did not match the TableVersion etag in the table.
        /// </summary>
        /// <param name="entry">MembershipEntry to be inserted.</param>
        /// <param name="tableVersion">The new TableVersion for this table, along with its etag.</param>
        /// <returns>True if the insert operation succeeded and false otherwise.</returns>
        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3(
                    string.Format(
                        "MongoMembershipTable.InsertRow called with entry {0} and tableVersion {1}.",
                        entry,
                        tableVersion));
            }

            // The "tableVersion" parameter should always exist when inserting a row as Init should
            // have been called and membership version created and read. This is an optimization to
            // not to go through all the way to database to fail a conditional check on etag (which does
            // exist for the sake of robustness) as mandated by Orleans membership protocol.
            // Likewise, no update can be done without membership entry.
            if (entry == null)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose(
                        "MongoMembershipTable.InsertRow aborted due to null check. MembershipEntry is null.");
                }

                throw new ArgumentNullException("entry");
            }

            if (tableVersion == null)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose(
                        "MongoMembershipTable.InsertRow aborted due to null check. TableVersion is null ");
                }

                throw new ArgumentNullException("tableVersion");
            }

            try
            {
                return await this.repository.InsertMembershipRow(this.deploymentId, entry, tableVersion);
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("MongoMembershipTable.InsertRow failed: {0}", ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Atomically tries to update the MembershipEntry for one silo and also update the TableVersion.
        /// If operation succeeds, the following changes would be made to the table:
        /// 1) The MembershipEntry for this silo will be updated to the new MembershipEntry (the old entry will be fully substitued by the new entry)
        /// 2) The eTag for the updated MembershipEntry will also be eTag with the new unique automatically generated eTag.
        /// 3) TableVersion.Version in the table will be updated to the new TableVersion.Version.
        /// 4) TableVersion etag in the table will be updated to the new unique automatically generated eTag.
        /// All those changes to the table, update of a new row and update of the table version and the associated etags, should happen atomically, or fail atomically with no side effects.
        /// The operation should fail in each of the following conditions:
        /// 1) A MembershipEntry for a given silo does not exist in the table
        /// 2) A MembershipEntry for a given silo exist in the table but its etag in the table does not match the provided etag.
        /// 3) Update of the TableVersion failed since the given TableVersion etag (as specified by the TableVersion.VersionEtag property) did not match the TableVersion etag in the table.
        /// </summary>
        /// <param name="entry">MembershipEntry to be updated.</param>
        /// <param name="etag">The etag  for the given MembershipEntry.</param>
        /// <param name="tableVersion">The new TableVersion for this table, along with its etag.</param>
        /// <returns>True if the update operation succeeded and false otherwise.</returns>
        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the IAmAlive part (column) of the MembershipEntry for this silo.
        /// This operation should only update the IAmAlive collumn and not change other columns.
        /// This operation is a "dirty write" or "in place update" and is performed without etag validation.
        /// With regards to eTags update:
        /// This operation may automatically update the eTag associated with the given silo row, but it does not have to. It can also leave the etag not changed ("dirty write").
        /// With regards to TableVersion:
        /// this operation should not change the TableVersion of the table. It should leave it untouched.
        /// There is no scenario where this operation could fail due to table semantical reasons. It can only fail due to network problems or table unavailability.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>Task representing the successful execution of this operation. </returns>
        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3(string.Format("MongoMembershipTable.UpdateIAmAlive called with entry {0}.", entry));
            }

            if (entry == null)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose(
                        "MongoMembershipTable.UpdateIAmAlive aborted due to null check. MembershipEntry is null.");
                }

                throw new ArgumentNullException("entry");
            }

            try
            {
                await this.repository.UpdateIAmAliveTimeAsyncTask( 
                        this.deploymentId,
                        entry.SiloAddress,
                        entry.IAmAliveTime);
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose)
                {
                    this.logger.Verbose("MongoMembershipTable.UpdateIAmAlive failed: {0}", ex);
                }

                throw;
            }

        }

        #endregion

        #region Implementation of IGatewayListProvider

        /// <summary>
        /// Initializes the provider, will be called before all other methods
        /// </summary>
        /// <param name="clientConfiguration">the given client configuration</param>
        /// <param name="traceLogger">the logger to be used by the provider</param>
        public Task InitializeGatewayListProvider(ClientConfiguration clientConfiguration, TraceLogger traceLogger)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the list of gateways (silos) that can be used by a client to connect to Orleans cluster.
        /// The Uri is in the form of: "gwy.tcp://IP:port/Generation". See Utils.ToGatewayUri and Utils.ToSiloAddress for more details about Uri format.
        /// </summary>
        public Task<IList<Uri>> GetGateways()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether is updatable.
        /// </summary>
        public bool IsUpdatable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the max staleness.
        /// </summary>
        public TimeSpan MaxStaleness { get; private set; }

        #endregion

        /// <summary>
        /// The init table async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task<bool> InitTableAsync()
        {
            try
            {
                await this.repository.InitMembershipVersionCollectionAsync(this.deploymentId);
                return true;
            }
            catch (Exception ex)
            {
                if (this.logger.IsVerbose2)
                {
                    this.logger.Verbose2("Insert silo membership version failed: {0}", ex.ToString());
                }

                throw;
            }
        }
    }
}
