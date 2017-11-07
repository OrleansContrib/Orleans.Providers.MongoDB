using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.UnitTest.Storage
{
    partial class MongoStorageProviderTests
    {
        public sealed class StatefulGrainState
        {
            public int Counter;
        }

        public interface IStatefulGrain : IGrainWithGuidKey
        {
            Task Increment();

            Task Decrement();

            Task Clear();

            Task<int> Count();

            Task Teardown();
        }

        [StorageProvider(ProviderName = "Default")]
        public sealed class StatefulGrain : Grain<StatefulGrainState>, IStatefulGrain
        {
            public Task<int> Count()
            {
                return Task.FromResult(State.Counter);
            }

            public Task Decrement()
            {
                State.Counter--;

                return Task.CompletedTask;
            }

            public Task Increment()
            {
                State.Counter++;

                return Task.CompletedTask;
            }

            public async Task Teardown()
            {
                await WriteStateAsync();

                DeactivateOnIdle();
            }

            public Task Clear()
            {
                return ClearStateAsync();
            }
        }
    }
}
