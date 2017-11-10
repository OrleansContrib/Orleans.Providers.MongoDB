using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Membership.Store;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

// ReSharper disable ConvertToLambdaExpression

namespace Orleans.Providers.MongoDB.Membership
{
    public sealed class MongoGatewayListProvider : IGatewayListProvider
    {
        private readonly ILogger<MongoGatewayListProvider> logger;
        private readonly ClientConfiguration config;
        private readonly MongoMembershipCollection gatewaysCollection;

        /// <inheritdoc />
        public bool IsUpdatable { get; } = true;

        /// <inheritdoc />
        public TimeSpan MaxStaleness => config.GatewayListRefreshPeriod;

        public MongoGatewayListProvider(
            ILogger<MongoGatewayListProvider> logger,
            IOptions<MongoDBGatewayListProviderOptions> options,
            ClientConfiguration config)
        {
            this.logger = logger;
            this.config = config;

            options.Value.EnrichAndValidate(config);

            gatewaysCollection =
                new MongoMembershipCollection(
                    options.Value.ConnectionString,
                    options.Value.DatabaseName,
                    options.Value.CollectionPrefix);
        }

        /// <inheritdoc />
        public Task InitializeGatewayListProvider()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IList<Uri>> GetGateways()
        {
            return DoAndLog(nameof(GetGateways), () =>
            {
                return gatewaysCollection.GetGateways(config.DeploymentId);
            });
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action)
        {
            logger.LogInformation($"{nameof(MongoGatewayListProvider)}.{actionName} called.");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.Warn((int)MongoProviderErrorCode.MembershipTable_Operations, $"{nameof(MongoGatewayListProvider)}.{actionName} failed. Exception={ex.Message}", ex);

                throw;
            }
        }
    }
}
