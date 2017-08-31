using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Membership.Repository
{
    public interface IGatewayProviderRepository
    {
        /// <summary>
        ///     Returns active gateways.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        Task<List<Uri>> ReturnActiveGatewaysAsync(string deploymentId);
    }
}