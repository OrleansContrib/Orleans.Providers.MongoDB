using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Streams;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class StreamConsumerGrain : Grain, IStreamConsumerGrain
    {
        private readonly ILogger<StreamConsumerGrain> logger;
        private int consumedItems = 0;

        public StreamConsumerGrain(ILogger<StreamConsumerGrain> logger)
        {
            this.logger = logger;
        }

        public async Task Activate()
        {
            var streamProvider = this.GetStreamProvider("OrleansTestStream");
            var streamOfNumbers = streamProvider.GetStream<int>("MyNamespace", Guid.Empty);

            await streamOfNumbers.SubscribeAsync((message, token) =>
            {
                this.logger.LogInformation("Grain {GrainId} consumed: {Message}", this.GetPrimaryKeyString(), message);
                consumedItems++;

                return Task.CompletedTask;
            });
        }

        public Task<int> GetConsumedItems()
        {
            return Task.FromResult(consumedItems);
        }
    }
}
