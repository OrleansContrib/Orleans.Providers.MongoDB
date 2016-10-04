namespace Orleans.Providers.MongoDB.Membership
{
    #region Using

    using System;
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

        Task InitMembershipVersionCollectionAsync(string deploymentId);

        Task<bool> InsertMembershipRow(string deploymentId, MembershipEntry entry, TableVersion tableVersion);

        Task<MembershipTableData> ReturnMembershipTableData(string deploymentId);

        Task<MembershipTableData> ReturnRow(SiloAddress key, string deploymentId);

        Task UpdateIAmAliveTimeAsyncTask(string deploymentId, SiloAddress siloAddress, DateTime iAmAliveTime);

        Task<bool> UpdateMembershipRowAsync(string deploymentId, MembershipEntry membershipEntry, string etag);

        #endregion
    }
}