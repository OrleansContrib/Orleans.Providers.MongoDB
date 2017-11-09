
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.UnitTest.Statistics
{
    internal class DummyCounter : ICounter
    {
        public string Name => "DummyCounter";

        public bool IsValueDelta => true;

        public string GetValueString()
        {
            return "GetValueString";
        }

        public string GetDeltaString()
        {
            return "GetDeltaString";
        }

        public string GetDisplayString()
        {
            return "GetDisplayString";
        }

        public void ResetCurrent()
        {
        }

        public CounterStorage Storage => CounterStorage.LogAndTable;

        public void TrackMetric(Logger telemetryProducer)
        {
        }
    }
}