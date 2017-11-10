using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Orleans.Providers.MongoDB.Utils
{
    internal static class Guard
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object target, string parameterName)
        {
            if (target == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
