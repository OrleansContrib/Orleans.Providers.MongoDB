using System;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public static class EtagHelper
    {
        public static string CreateNew()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
