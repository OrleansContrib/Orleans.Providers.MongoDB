namespace Orleans.Providers.MongoDB.Membership.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using global::MongoDB.Driver;
    using global::MongoDB.Driver.Linq;

    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    public class GatewayProviderRepository : DocumentRepository, IGatewayProviderRepository
    {
        public async Task<List<Uri>> ReturnActiveGatewaysAsync(string deploymentId)
        {
            var collection = Database.GetCollection<MembershipTable>(MongoMembershipProviderRepository.MembershipCollectionName);

            // Todo: This should be async 
            List<MembershipTable> gateways =
                collection.AsQueryable()
                    .Where(m => m.DeploymentId == deploymentId && m.Status == (int)SiloStatus.Active && m.ProxyPort > 0)
                    .ToList();

            List<Uri> results = new List<Uri>();

            foreach (var gateway in gateways)
            {
                results.Add(ReturnGatewayUri(gateway));
            }

            return results;
        }

        internal static Uri ReturnGatewayUri(MembershipTable record)
        {
            return MongoMembershipProviderRepository.ReturnSiloAddress(record, true).ToGatewayUri();
        }

        public GatewayProviderRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {
        }
    }
}
