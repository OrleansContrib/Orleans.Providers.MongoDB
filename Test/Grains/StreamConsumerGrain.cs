using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class StreamConsumerGrain : Grain, IStreamConsumerGrain
    {
        private int consumedItems = 0;

        public async Task Activate()
        {
            var streamProvider = GetStreamProvider("OrleansTestStream");
            var streamOfNumbers = streamProvider.GetStream<int>(Guid.Empty, "MyNamespace");

            await streamOfNumbers.SubscribeAsync((message, token) =>
            {
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
