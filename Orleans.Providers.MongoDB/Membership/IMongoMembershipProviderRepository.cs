namespace Orleans.Providers.MongoDB.Membership
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    #endregion

    /// <summary>
    /// The MongoMembershipProviderRepository interface.
    /// </summary>
    internal interface IMongoMembershipProviderRepository : IDocumentRepository
    {
        #region Public methods and operators

        /// <summary>
        /// The init membership version collection async.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task InitMembershipVersionCollectionAsync(string deploymentId);

        /// <summary>
        /// The insert membership row.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <param name="entry">
        /// The entry.
        /// </param>
        /// <param name="tableVersion">
        /// The table version.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<bool> InsertMembershipRow(string deploymentId, MembershipEntry entry, TableVersion tableVersion);

        /// <summary>
        /// The return membership table data.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<MembershipTableData> ReturnMembershipTableData(string deploymentId);

        /// <summary>
        /// The return row.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<MembershipTableData> ReturnRow(SiloAddress key, string deploymentId);

        /// <summary>
        /// The update i am alive time async task.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <param name="siloAddress">
        /// The silo address.
        /// </param>
        /// <param name="iAmAliveTime">
        /// The i am alive time.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task UpdateIAmAliveTimeAsyncTask(string deploymentId, SiloAddress siloAddress, DateTime iAmAliveTime);

        /// <summary>
        /// The update membership row async.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <param name="membershipEntry">
        /// The membership entry.
        /// </param>
        /// <param name="etag">
        /// The etag.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<bool> UpdateMembershipRowAsync(string deploymentId, MembershipEntry membershipEntry, string etag);

        #endregion
    }
}