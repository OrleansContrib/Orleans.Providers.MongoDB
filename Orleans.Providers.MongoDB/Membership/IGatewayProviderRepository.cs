namespace Orleans.Providers.MongoDB.Membership
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IGatewayProviderRepository
    {
        Task<List<Uri>> ReturnActiveGatewaysAsync(string deploymentId);
    }
}
