using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using System;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class StreamProducerGrain : Grain, IStreamProducerGrain
    {
        public async Task ProduceEvents()
        {
            var streamProvider = GetStreamProvider("OrleansTestStream");
            var streamOfNumbers = streamProvider.GetStream<int>(Guid.Empty, "MyNamespace");

            var i = 0;

            while (true)
            {
                await streamOfNumbers.OnNextAsync(++i);

                await Task.Delay(100);
            }
        }
    }
}
