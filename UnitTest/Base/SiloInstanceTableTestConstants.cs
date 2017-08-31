using System;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    internal static class SiloInstanceTableTestConstants
    {
        internal static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

        internal static readonly bool DeleteEntriesAfterTest = true; // false; // Set to false for Debug mode

        internal static readonly string INSTANCE_STATUS_CREATED = SiloStatus.Created.ToString(); //"Created";
        internal static readonly string INSTANCE_STATUS_ACTIVE = SiloStatus.Active.ToString(); //"Active";
        internal static readonly string INSTANCE_STATUS_DEAD = SiloStatus.Dead.ToString(); //"Dead";
    }
}