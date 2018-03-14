using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Membership.Store;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Configuration;

// ReSharper disable ConvertToLambdaExpression

namespace Orleans.Providers.MongoDB.Membership
{
    public sealed class MongoGatewayListProvider : IGatewayListProvider
    {
        private readonly ILogger<MongoGatewayListProvider> logger;
        private readonly MongoDBGatewayListProviderOptions options;
        private readonly TimeSpan maxStaleness;
        private readonly string clusterId;
        private MongoMembershipCollection gatewaysCollection;

        /// <inheritdoc />
        public bool IsUpdatable { get; } = true;

        /// <inheritdoc />
        public TimeSpan MaxStaleness => maxStaleness;

        public MongoGatewayListProvider(
            ILogger<MongoGatewayListProvider> logger,
            IOptions<ClusterOptions> clusterOptions,
            IOptions<GatewayOptions> gatewayOptions,
            IOptions<MongoDBGatewayListProviderOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;
            this.clusterId = clusterOptions.Value.ClusterId;
            this.maxStaleness = gatewayOptions.Value.GatewayListRefreshPeriod;
        }

        /// <inheritdoc />
        public Task InitializeGatewayListProvider()
        {
            gatewaysCollection =
                new MongoMembershipCollection(
                    options.ConnectionString,
                    options.DatabaseName,
                    options.CollectionPrefix);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IList<Uri>> GetGateways()
        {
            return DoAndLog(nameof(GetGateways), () =>
            {
                return gatewaysCollection.GetGateways(clusterId);
            });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            logger.LogDebug($"{nameof(MongoGatewayListProvider)}.{actionName} called.");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogWarning((int)MongoProviderErrorCode.MembershipTable_Operations, ex, $"{nameof(MongoGatewayListProvider)}.{actionName} failed. Exception={ex.Message}");

                throw;
            }
        }
    }
}
