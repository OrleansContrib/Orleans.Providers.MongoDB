using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Membership
{
    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    interface IMongoMembershipProviderRepository : IDocumentRepository
    {
        /// <summary>
        /// The init membership version collection async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task InitMembershipVersionCollectionAsync(string deploymentId);

        Task<MembershipTableData> ReturnMembershipTableData(string deploymentId, string suspectingSilos);

        Task<MembershipTableData> ReturnRow(SiloAddress key, string deploymentId, string suspectingSilos);

        Task<bool> InsertMembershipRow(string deploymentId, MembershipEntry entry, TableVersion tableVersion);

        Task UpdateIAmAliveTimeAsyncTask(string deploymentId, SiloAddress siloAddress, DateTime iAmAliveTime);
    }
}
