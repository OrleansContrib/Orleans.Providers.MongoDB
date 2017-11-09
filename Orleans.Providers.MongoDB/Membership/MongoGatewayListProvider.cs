using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Membership.Store;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

// ReSharper disable ConvertToLambdaExpression

namespace Orleans.Providers.MongoDB.Membership
{
    public sealed class MongoGatewayListProvider : IGatewayListProvider
    {
        private readonly ILogger<MongoGatewayListProvider> logger;
        private readonly ClientConfiguration config;
        private MongoMembershipCollection gatewaysCollection;

        /// <inheritdoc />
        public bool IsUpdatable { get; } = true;

        /// <inheritdoc />
        public TimeSpan MaxStaleness { get; }

        public MongoGatewayListProvider(ILogger<MongoGatewayListProvider> logger, ClientConfiguration config)
        {
            this.logger = logger;
            this.config = config;

            MaxStaleness = config.GatewayListRefreshPeriod;
        }

        public Task InitializeGatewayListProvider()
        {
            return DoAndLog(nameof(InitializeGatewayListProvider), () =>
            {
                gatewaysCollection =
                    new MongoMembershipCollection(config.DataConnectionString,
                        MongoUrl.Create(config.DataConnectionString).DatabaseName);

                return Task.CompletedTask;
            });
        }

        /// <inheritdoc />
        public Task<IList<Uri>> GetGateways()
        {
            return DoAndLog(nameof(GetGateways), () =>
            {
                return gatewaysCollection.GetGateways(config.DeploymentId);
            });
        }

        private Task DoAndLog(string actionName, Func<Task> action)
        {
            return DoAndLog(actionName, async () => { await action(); return true; });
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
