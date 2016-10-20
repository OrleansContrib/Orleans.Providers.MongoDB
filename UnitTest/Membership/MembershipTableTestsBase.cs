using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Messaging;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace UnitTests.MembershipTests
{
    using System.Diagnostics;
    using System.Reflection;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Orleans.Providers.MongoDB.UnitTest.Membership;
    using Orleans.Providers.MongoDB.UnitTest.Reminders;
    using Orleans.TestingHost;
    using Orleans.TestingHost.Utils;

    public abstract class MembershipTableTestsBase : IDisposable
    {
        private static readonly string hostName = Dns.GetHostName();
        private readonly Logger logger;
        private readonly IMembershipTable membershipTable;
        private readonly IGatewayListProvider gatewayListProvider;
        private readonly string deploymentId;

        protected const string testDatabaseName = "OrleansMembershipTest";//for relational storage

        private const bool IsTrue = true;

        protected MembershipTableTestsBase(ClusterConfiguration clusterConfiguration)
        {
            ConstructorInfo loggerGlobalConfiguration = typeof(Logger).GetConstructors
           (BindingFlags.Instance | BindingFlags.NonPublic)[0];

            //logger = (TraceLogger)loggerGlobalConfiguration.Invoke(new object[] { GetType().Name, TraceLogger.LoggerType.Application });

            //logger = (TraceLogger)loggerGlobalConfiguration.Invoke(new object[] {});

            this.logger = new NoOpTestLogger();

            //LogManager.Initialize(new NodeConfiguration());
            //logger = LogManager.GetLogger(GetType().Name, TraceLogger.LoggerType.Application);

            //logger = new TraceLogger(GetType().Name, TraceLogger.LoggerType.Application);

            //        MethodInfo dynMethod = this.GetType().GetMethod("Draw_" + itemType,
            //BindingFlags.NonPublic | BindingFlags.Instance);
            //        dynMethod.Invoke(this, new object[] { methodParams });

            ConstructorInfo ctorGlobalConfiguration = typeof(GlobalConfiguration).GetConstructors
            (BindingFlags.Instance | BindingFlags.NonPublic)[0];

            GlobalConfiguration globalConfiguration = (GlobalConfiguration)ctorGlobalConfiguration.Invoke(new object[] {});
            globalConfiguration.DeploymentId = globalConfiguration.DeploymentId;
            //globalConfiguration.AdoInvariant = GetAdoInvariant();
            globalConfiguration.DataConnectionString = clusterConfiguration.Globals.DataConnectionString;

            deploymentId = clusterConfiguration.Globals.DeploymentId;

            globalConfiguration.LivenessType = clusterConfiguration.Globals.LivenessType;
            globalConfiguration.MembershipTableAssembly = clusterConfiguration.Globals.MembershipTableAssembly;
            globalConfiguration.ReminderServiceType = clusterConfiguration.Globals.ReminderServiceType;
            globalConfiguration.DeploymentId = clusterConfiguration.Globals.DeploymentId;
            
            membershipTable = CreateMembershipTable(logger);
            membershipTable.InitializeMembershipTable(globalConfiguration, IsTrue, logger).WithTimeout(TimeSpan.FromMinutes(1)).Wait();

            var clientConfiguration = new ClientConfiguration
            {
                DeploymentId = globalConfiguration.DeploymentId,
                AdoInvariant = globalConfiguration.AdoInvariant,
                DataConnectionString = globalConfiguration.DataConnectionString
            };

            gatewayListProvider = CreateGatewayListProvider(logger);
            gatewayListProvider.InitializeGatewayListProvider(clientConfiguration, logger).WithTimeout(TimeSpan.FromMinutes(1)).Wait();
        }

        public void Dispose()
        {
            if (membershipTable != null && SiloInstanceTableTestConstants.DeleteEntriesAfterTest)
            {
                membershipTable.DeleteMembershipTableEntries(deploymentId).Wait();
            }
        }

        protected abstract IGatewayListProvider CreateGatewayListProvider(Logger logger);
        protected abstract IMembershipTable CreateMembershipTable(Logger logger);

        //protected virtual string GetAdoInvariant()
        //{
        //    return null;
        //}

        protected async Task MembershipTable_GetGateways()
        {
            var membershipEntries = Enumerable.Range(0, 10).Select(i => CreateMembershipEntryForTest()).ToArray();

            membershipEntries[3].Status = SiloStatus.Active;
            membershipEntries[3].ProxyPort = 0;
            membershipEntries[5].Status = SiloStatus.Active;
            membershipEntries[9].Status = SiloStatus.Active;

            var data = await membershipTable.ReadAll();
            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Members.Count);

            var version = data.Version;
            foreach (var membershipEntry in membershipEntries)
            {
                Assert.IsTrue(await membershipTable.InsertRow(membershipEntry, version));
                version = (await membershipTable.ReadRow(membershipEntry.SiloAddress)).Version;
            }

            var gateways = await gatewayListProvider.GetGateways();

            var entries = new List<string>(gateways.Select(g => g.ToString()));

            Assert.IsTrue(entries.Contains(membershipEntries[5].SiloAddress.ToGatewayUri().ToString()));
            Assert.IsTrue(entries.Contains(membershipEntries[9].SiloAddress.ToGatewayUri().ToString()));
        }

        protected async Task MembershipTable_ReadAll_EmptyTable()
        {
            var data = await membershipTable.ReadAll();
            Assert.IsNotNull(data);

            logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.AreEqual(0, data.Members.Count);
            Assert.IsNotNull(data.Version.VersionEtag);
            Assert.AreEqual(0, data.Version.Version);
        }

        protected async Task MembershipTable_InsertRow(bool extendedProtocol = IsTrue)
        {
            var membershipEntry = CreateMembershipEntryForTest();

            var data = await membershipTable.ReadAll();
            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Members.Count);

            TableVersion nextTableVersion = data.Version.Next();

            bool ok = await membershipTable.InsertRow(membershipEntry, nextTableVersion);
            Assert.IsTrue(ok, "InsertRow failed");

            data = await membershipTable.ReadAll();

            if (extendedProtocol)
                Assert.AreEqual(1, data.Version.Version);

            Assert.AreEqual(1, data.Members.Count);
        }

        protected async Task MembershipTable_ReadRow_Insert_Read(bool extendedProtocol = IsTrue)
        {
            MembershipTableData data = await membershipTable.ReadAll();

            logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.AreEqual(0, data.Members.Count);

            TableVersion newTableVersion = data.Version.Next();

            MembershipEntry newEntry = CreateMembershipEntryForTest();
            bool ok = await membershipTable.InsertRow(newEntry, newTableVersion);

            Assert.IsTrue(ok, "InsertRow failed");

            ok = await membershipTable.InsertRow(newEntry, newTableVersion);
            Assert.IsFalse(ok, "InsertRow should have failed - same entry, old table version");

            if (extendedProtocol)
            {
                ok = await membershipTable.InsertRow(CreateMembershipEntryForTest(), newTableVersion);
                Assert.IsFalse(ok, "InsertRow should have failed - new entry, old table version");
            }

            data = await membershipTable.ReadAll();

            if (extendedProtocol)
                Assert.AreEqual(1, data.Version.Version);

            TableVersion nextTableVersion = data.Version.Next();

            ok = await membershipTable.InsertRow(newEntry, nextTableVersion);
            Assert.IsFalse(ok, "InsertRow should have failed - duplicate entry");

            data = await membershipTable.ReadAll();

            Assert.AreEqual(1, data.Members.Count);

            data = await membershipTable.ReadRow(newEntry.SiloAddress);
            if (extendedProtocol)
                Assert.AreEqual(newTableVersion.Version, data.Version.Version);

            logger.Info("Membership.ReadRow returned VableVersion={0} Data={1}", data.Version, data);

            Assert.AreEqual(1, data.Members.Count);
            Assert.IsNotNull(data.Version.VersionEtag);
            if (extendedProtocol)
            {
                Assert.AreNotEqual(newTableVersion.VersionEtag, data.Version.VersionEtag);
                Assert.AreEqual(newTableVersion.Version, data.Version.Version);
            }
            var membershipEntry = data.Members[0].Item1;
            string eTag = data.Members[0].Item2;
            logger.Info("Membership.ReadRow returned MembershipEntry ETag={0} Entry={1}", eTag, membershipEntry);

            Assert.IsNotNull(eTag);
            Assert.IsNotNull(membershipEntry);
        }

        protected async Task MembershipTable_ReadAll_Insert_ReadAll(bool extendedProtocol = IsTrue)
        {
            MembershipTableData data = await membershipTable.ReadAll();
            logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.AreEqual(0, data.Members.Count);

            TableVersion newTableVersion = data.Version.Next();

            MembershipEntry newEntry = CreateMembershipEntryForTest();
            bool ok = await membershipTable.InsertRow(newEntry, newTableVersion);

            Assert.IsTrue(ok, "InsertRow failed");

            data = await membershipTable.ReadAll();
            logger.Info("Membership.ReadAll returned VableVersion={0} Data={1}", data.Version, data);

            Assert.AreEqual(1, data.Members.Count);
            Assert.IsNotNull(data.Version.VersionEtag);

            if (extendedProtocol)
            {
                Assert.AreNotEqual(newTableVersion.VersionEtag, data.Version.VersionEtag);
                Assert.AreEqual(newTableVersion.Version, data.Version.Version);
            }

            var membershipEntry = data.Members[0].Item1;
            string eTag = data.Members[0].Item2;
            logger.Info("Membership.ReadAll returned MembershipEntry ETag={0} Entry={1}", eTag, membershipEntry);

            Assert.IsNotNull(eTag);
            Assert.IsNotNull(membershipEntry);
        }

        protected async Task MembershipTable_UpdateRow(bool extendedProtocol = IsTrue)
        {
            var tableData = await membershipTable.ReadAll();
            Assert.IsNotNull(tableData.Version);

            Assert.AreEqual(0, tableData.Version.Version);
            Assert.AreEqual(0, tableData.Members.Count);

            for (int i = 1; i < 10; i++)
            {
                var siloEntry = CreateMembershipEntryForTest();

                siloEntry.SuspectTimes =
                    new List<Tuple<SiloAddress, DateTime>>
                    {
                        new Tuple<SiloAddress, DateTime>(CreateSiloAddressForTest(), GetUtcNowWithSecondsResolution().AddSeconds(1)),
                        new Tuple<SiloAddress, DateTime>(CreateSiloAddressForTest(), GetUtcNowWithSecondsResolution().AddSeconds(2))
                    };

                TableVersion tableVersion = tableData.Version.Next();

                logger.Info("Calling InsertRow with Entry = {0} TableVersion = {1}", siloEntry, tableVersion);
                bool ok = await membershipTable.InsertRow(siloEntry, tableVersion);

                Assert.IsTrue(ok, "InsertRow failed");

                tableData = await membershipTable.ReadAll();

                var etagBefore = tableData.Get(siloEntry.SiloAddress).Item2;

                Assert.IsNotNull(etagBefore);

                if (extendedProtocol)
                {
                    logger.Info("Calling UpdateRow with Entry = {0} correct eTag = {1} old version={2}", siloEntry,
                                etagBefore, tableVersion != null ? tableVersion.ToString() : "null");
                    ok = await membershipTable.UpdateRow(siloEntry, etagBefore, tableVersion);
                    Assert.IsFalse(ok, $"row update should have failed - Table Data = {tableData}");
                    tableData = await membershipTable.ReadAll();
                }

                tableVersion = tableData.Version.Next();

                logger.Info("Calling UpdateRow with Entry = {0} correct eTag = {1} correct version={2}", siloEntry,
                    etagBefore, tableVersion != null ? tableVersion.ToString() : "null");

                ok = await membershipTable.UpdateRow(siloEntry, etagBefore, tableVersion);

                Assert.IsTrue(ok, $"UpdateRow failed - Table Data = {tableData}");

                logger.Info("Calling UpdateRow with Entry = {0} old eTag = {1} old version={2}", siloEntry,
                    etagBefore, tableVersion != null ? tableVersion.ToString() : "null");
                ok = await membershipTable.UpdateRow(siloEntry, etagBefore, tableVersion);
                Assert.IsFalse(ok, $"row update should have failed - Table Data = {tableData}");

                tableData = await membershipTable.ReadAll();

                var tuple = tableData.Get(siloEntry.SiloAddress);

                Assert.AreEqual(ToFullString(tuple.Item1, IsTrue), ToFullString(siloEntry, IsTrue));

                var etagAfter = tuple.Item2;

                if (extendedProtocol)
                {
                    logger.Info("Calling UpdateRow with Entry = {0} correct eTag = {1} old version={2}", siloEntry,
                                etagAfter, tableVersion != null ? tableVersion.ToString() : "null");

                    ok = await membershipTable.UpdateRow(siloEntry, etagAfter, tableVersion);

                    Assert.IsFalse(ok, $"row update should have failed - Table Data = {tableData}");
                }

                tableData = await membershipTable.ReadAll();

                etagBefore = etagAfter;

                etagAfter = tableData.Get(siloEntry.SiloAddress).Item2;

                Assert.AreEqual(etagBefore, etagAfter);
                Assert.IsNotNull(tableData.Version);
                if (extendedProtocol)
                    Assert.AreEqual(tableVersion.Version, tableData.Version.Version);

                Assert.AreEqual(i, tableData.Members.Count);
            }
        }

        internal string ToFullString(MembershipEntry entry, bool full = false)
        {
            if (!full)
                return ToString();

            List<SiloAddress> suspecters = entry.SuspectTimes == null
                ? null
                : entry.SuspectTimes.Select(tuple => tuple.Item1).ToList();
            List<DateTime> timestamps = entry.SuspectTimes == null
                ? null
                : entry.SuspectTimes.Select(tuple => tuple.Item2).ToList();
            return string.Format("[SiloAddress={0} SiloName={1} Status={2} HostName={3} ProxyPort={4} " +
                                 "RoleName={5} UpdateZone={6} FaultZone={7} StartTime = {8} IAmAliveTime = {9} {10} {11}]",
                entry.SiloAddress.ToLongString(),
                entry.HostName,
                entry.Status,
                entry.HostName,
                entry.ProxyPort,
                entry.RoleName,
                entry.UpdateZone,
                entry.FaultZone,
                LogFormatter.PrintDate(entry.StartTime),
                LogFormatter.PrintDate(entry.IAmAliveTime),
                suspecters == null
                    ? ""
                    : "Suspecters = " + Utils.EnumerableToString(suspecters, sa => sa.ToLongString()),
                timestamps == null
                    ? ""
                    : "SuspectTimes = " + Utils.EnumerableToString(timestamps, LogFormatter.PrintDate)
                );
        }

        protected async Task MembershipTable_UpdateRowInParallel(bool extendedProtocol = IsTrue)
        {
            var tableData = await membershipTable.ReadAll();

            var data = CreateMembershipEntryForTest();

            TableVersion newTableVer = tableData.Version.Next();

            var insertions = Task.WhenAll(Enumerable.Range(1, 20).Select(i => membershipTable.InsertRow(data, newTableVer)));

            Assert.IsTrue((await insertions).Single(x => x), "InsertRow failed");

            await Task.WhenAll(Enumerable.Range(1, 19).Select(async i =>
            {
                bool done;
                do
                {
                    var updatedTableData = await membershipTable.ReadAll();
                    var updatedRow = updatedTableData.Get(data.SiloAddress);

                    TableVersion tableVersion = updatedTableData.Version.Next();

                    await Task.Delay(10);
                    done = await membershipTable.UpdateRow(updatedRow.Item1, updatedRow.Item2, tableVersion);
                } while (!done);
            })).WithTimeout(TimeSpan.FromSeconds(30));


            tableData = await membershipTable.ReadAll();
            Assert.IsNotNull(tableData.Version);

            if (extendedProtocol)
                Assert.AreEqual(20, tableData.Version.Version);

            Assert.AreEqual(1, tableData.Members.Count);
        }

        private static int generation;
        // Utility methods
        private static MembershipEntry CreateMembershipEntryForTest()
        {
            SiloAddress siloAddress = CreateSiloAddressForTest();


            var membershipEntry = new MembershipEntry
            {
                SiloAddress = siloAddress,
                HostName = hostName,
                //SiloName = "TestSiloName",
                Status = SiloStatus.Joining,
                ProxyPort = siloAddress.Endpoint.Port,
                StartTime = GetUtcNowWithSecondsResolution(),
                IAmAliveTime = GetUtcNowWithSecondsResolution()
            };

            return membershipEntry;
        }

        private static DateTime GetUtcNowWithSecondsResolution()
        {
            var now = DateTime.UtcNow;
            //return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            return now;
        }

        private static SiloAddress CreateSiloAddressForTest()
        {
            var siloAddress = SiloAddress.NewLocalAddress(Interlocked.Increment(ref generation));
            siloAddress.Endpoint.Port = 12345;
            return siloAddress;
        }
    }
}
