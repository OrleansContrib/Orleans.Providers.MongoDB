using System.Net;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership
{
    public class MembershipHelper
    {
        /// <summary>
        /// Returns a silo address.
        /// </summary>
        /// <param name="membershipData">
        /// The membership data.
        /// </param>
        /// <param name="useProxyPort">
        /// The use proxy port.
        /// </param>
        /// <returns>
        /// The <see cref="SiloAddress"/>.
        /// </returns>
        public static SiloAddress ReturnSiloAddress(MembershipCollection membershipData, bool useProxyPort = false)
        {
            int port = membershipData.Port;

            if (useProxyPort)
            {
                port = membershipData.ProxyPort;
            }

            int generation = membershipData.Generation;
            string address = membershipData.Address;
            var siloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Parse(address), port), generation);
            return siloAddress;
        }

        /// <summary>
        /// Returns string from suspect times.
        /// </summary>
        /// <param name="membershipEntry">
        /// The membership entry.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        internal static string ReturnStringFromSuspectTimes(MembershipEntry membershipEntry)
        {
            if (membershipEntry.SuspectTimes != null)
            {
                string suspectingSilos = string.Empty;
                foreach (var suspectTime in membershipEntry.SuspectTimes)
                {
                    suspectingSilos += string.Format(
                        "{0}@{1},{2} |",
                        suspectTime.Item1.Endpoint,
                        suspectTime.Item1.Generation,
                        LogFormatter.PrintDate(suspectTime.Item2.ToUniversalTime()));
                }

                return suspectingSilos.TrimEnd('|').TrimEnd(' ');
            }

            return string.Empty;
        }

    }
}
