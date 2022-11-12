﻿using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Streams;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class StreamConsumerGrain : Grain, IStreamConsumerGrain
    {
        private int consumedItems = 0;

        public async Task Activate()
        {
            var streamProvider = this.GetStreamProvider("OrleansTestStream");
            var streamOfNumbers = streamProvider.GetStream<int>("MyNamespace", Guid.Empty);

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
