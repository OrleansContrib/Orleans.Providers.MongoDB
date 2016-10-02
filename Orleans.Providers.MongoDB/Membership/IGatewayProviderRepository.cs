using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Membership
{
    public interface IGatewayProviderRepository
    {
        /// <summary>
        /// The return active gateways async.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<List<Uri>> ReturnActiveGatewaysAsync(string deploymentId);

    }
}
