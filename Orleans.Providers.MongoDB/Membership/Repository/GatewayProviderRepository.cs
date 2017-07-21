using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Repository
{
    public class GatewayProviderRepository : DocumentRepository, IGatewayProviderRepository
    {
        /// <summary>
        /// Returns active gateways.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<List<Uri>> ReturnActiveGatewaysAsync(string deploymentId)
        {
            var collection = Database.GetCollection<MembershipCollection>(MongoMembershipRepository.MembershipCollectionName);

            IAsyncCursor<MembershipCollection> gatewaysCursor =
                await collection.FindAsync(m => m.DeploymentId == deploymentId && m.Status == (int)SiloStatus.Active &&
                                                m.ProxyPort > 0);

            List<MembershipCollection> gateways = await gatewaysCursor.ToListAsync();

            List<Uri> results = new List<Uri>();

            foreach (var gateway in gateways)
            {
                results.Add(ReturnGatewayUri(gateway));
            }

            return results;
        }

        /// <summary>
        /// Return gateway uri.
        /// </summary>
        /// <param name="record">
        /// The record.
        /// </param>
        /// <returns>
        /// The <see cref="Uri"/>.
        /// </returns>
        internal static Uri ReturnGatewayUri(MembershipCollection record)
        {
            return MembershipHelper.ReturnSiloAddress(record, true).ToGatewayUri();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GatewayProviderRepository"/> class.
        /// </summary>
        /// <param name="connectionsString">
        /// The connections string.
        /// </param>
        /// <param name="databaseName">
        /// The database name.
        /// </param>
        public GatewayProviderRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {
        }
    }
}
