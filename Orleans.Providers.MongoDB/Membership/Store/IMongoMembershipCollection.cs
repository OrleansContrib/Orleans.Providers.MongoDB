using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public interface IMongoMembershipCollection
    {
        Task CleanupDefunctSiloEntries(string deploymentId, DateTimeOffset beforeDate);

        Task DeleteMembershipTableEntries(string deploymentId);

        Task<IList<Uri>> GetGateways(string deploymentId);

        Task<MembershipTableData> ReadAll(string deploymentId);

        Task<MembershipTableData> ReadRow(string deploymentId, SiloAddress address);

        Task UpdateIAmAlive(string deploymentId, SiloAddress address, DateTime iAmAliveTime);

        Task<bool> UpsertRow(string deploymentId, MembershipEntry entry, string etag, TableVersion tableVersion);
    }
}