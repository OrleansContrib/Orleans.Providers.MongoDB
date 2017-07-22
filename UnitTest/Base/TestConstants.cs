namespace Orleans.Providers.MongoDB.UnitTest.Base
{
    using System;
    using System.Diagnostics;

    using Orleans.Runtime;

    // used for test constants
    internal static class TestConstants
    {
        public static readonly SafeRandom random = new SafeRandom();

        public static readonly TimeSpan InitTimeout =
            Debugger.IsAttached ? TimeSpan.FromMinutes(15) : TimeSpan.FromMinutes(1);

    }
}
