using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.UnitTest.Statistics
{
    internal class DummyPerformanceMetrics : IClientPerformanceMetrics, ISiloPerformanceMetrics
    {
        public float CpuUsage => 1;

        public int SendQueueLength => 5;
        public int ReceiveQueueLength => 6;
        public int ActivationCount => 11;
        public int RecentlyUsedActivationCount => 12;

        public long AvailablePhysicalMemory => 2;
        public long MemoryUsage => 3;
        public long TotalPhysicalMemory => 4;
        public long SentMessages => 7;
        public long ReceivedMessages => 8;
        public long ConnectedGatewayCount => 9;
        public long RequestQueueLength => 10;
        public long ClientCount => 13;

        public bool IsOverloaded => false;

        public void LatchIsOverload(bool overloaded)
        {
        }

        public void UnlatchIsOverloaded()
        {
        }

        public void LatchCpuUsage(float value)
        {
        }

        public void UnlatchCpuUsage()
        {
        }
    }
}