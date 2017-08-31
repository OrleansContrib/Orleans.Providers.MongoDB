using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Repository
{
    #region Using

    #endregion

    /// <summary>
    ///     The MongoMembershipProviderRepository interface.
    /// </summary>
    internal interface IMongoMembershipRepository : IDocumentRepository
    {
        #region Public methods and operators

        /// <summary>
        ///     Init membership version collection.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task InitMembershipVersionCollectionAsync(string deploymentId);

        /// <summary>
        ///     Insert a membership as well as update the version
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <param name="entry">
        ///     The entry.
        /// </param>
        /// <param name="tableVersion">
        ///     The table version.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        Task<bool> InsertMembershipRow(string deploymentId, MembershipEntry entry, TableVersion tableVersion);

        /// <summary>
        ///     Return all the deployment members
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        Task<MembershipTableData> ReturnAllRows(string deploymentId);

        /// <summary>
        ///     Returns a membership for a deployment.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task<MembershipTableData> ReturnRow(SiloAddress key, string deploymentId);

        /// <summary>
        ///     Update i am alive time.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <param name="siloAddress">
        ///     The silo address.
        /// </param>
        /// <param name="iAmAliveTime">
        ///     The i am alive time.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task UpdateIAmAliveTimeAsyncTask(string deploymentId, SiloAddress siloAddress, DateTime iAmAliveTime);

        /// <summary>
        ///     Updates membership row.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <param name="membershipEntry">
        ///     The membership entry.
        /// </param>
        /// <param name="etag">
        ///     The etag.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task<bool> UpdateMembershipRowAsync(string deploymentId, MembershipEntry membershipEntry, string etag);

        /// <summary>
        ///     Deletes all memberships for a deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task DeleteMembershipTableEntriesAsync(string deploymentId);

        #endregion
    }
}