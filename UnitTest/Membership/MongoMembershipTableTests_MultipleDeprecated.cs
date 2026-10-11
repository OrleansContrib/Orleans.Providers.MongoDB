using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Clustering.TestKit;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using TestExtensions;
using UnitTests;
using UnitTests.MembershipTests;
using Xunit;
using Xunit.v3;

namespace Orleans.Providers.MongoDB.UnitTest.Membership
{
    public class MongoMembershipTableTests_MultipleDeprecated : MongoMembershipTableTestsBase, IAsyncLifetime
    {
        private const string SkipReason = "MultipleDeprecated does not support the extended membership protocol.";
        private static int generation;

        public MongoMembershipTableTests_MultipleDeprecated(ConnectionStringFixture fixture, TestEnvironmentFixture environment)
            : base(fixture, environment)
        {
        }

        protected override MongoDBMembershipStrategy Strategy => MongoDBMembershipStrategy.MultipleDeprecated;

        protected override IMongoClientFactory ClientFactory => MongoDatabaseFixture.DatabaseFactory;

        protected override Task<string> GetConnectionString()
        {
            return Task.FromResult(MongoDatabaseFixture.DatabaseConnectionString);
        }

        // The conformance suite verifies the extended membership protocol (table versions),
        // which this strategy intentionally does not implement.
        ValueTask IAsyncLifetime.InitializeAsync()
        {
            var declaringType = (TestContext.Current.TestMethod as IXunitTestMethod)?.Method.DeclaringType;

            if (declaringType == typeof(MembershipTableConformanceTestsBase) ||
                declaringType == typeof(MembershipTableFullConformanceTestsBase))
            {
                Assert.Skip(SkipReason);
            }

            return InitializeAsync();
        }

        protected override MembershipTableTestFixture CreateConformanceFixture()
        {
            throw Xunit.Sdk.SkipException.ForSkip(SkipReason);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_CleanupDefunctSiloEntries()
        {
            await MembershipTable_CleanupDefunctSiloEntries();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_GetGateways()
        {
            await MembershipTable_GetGateways();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_ReadAll_EmptyTable()
        {
            await MembershipTable_ReadAll_EmptyTable();
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_InsertRow_DuplicateFails()
        {
            var ct = TestContext.Current.CancellationToken;
            var table = await GetLegacyMembershipTableAsync(ct);
            var entry = CreateEntry();

            Assert.True(await table.InsertRowAsync(entry, (await table.ReadAllAsync(ct)).Version.Next(), ct));
            Assert.False(await table.InsertRowAsync(entry, (await table.ReadAllAsync(ct)).Version.Next(), ct));

            var data = await table.ReadRowAsync(entry.SiloAddress, ct);
            Assert.Equal(entry.SiloName, Assert.Single(data.Members).Item1.SiloName);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRow_RequiresCurrentEtag()
        {
            var ct = TestContext.Current.CancellationToken;
            var table = await GetLegacyMembershipTableAsync(ct);
            var entry = CreateEntry();

            Assert.True(await table.InsertRowAsync(entry, (await table.ReadAllAsync(ct)).Version.Next(), ct));

            var etag = Assert.Single((await table.ReadRowAsync(entry.SiloAddress, ct)).Members).Item2;

            entry.Status = SiloStatus.Active;
            Assert.True(await table.UpdateRowAsync(entry, etag, (await table.ReadAllAsync(ct)).Version.Next(), ct));

            entry.Status = SiloStatus.Dead;
            Assert.False(await table.UpdateRowAsync(entry, etag, (await table.ReadAllAsync(ct)).Version.Next(), ct));

            var row = Assert.Single((await table.ReadRowAsync(entry.SiloAddress, ct)).Members);
            Assert.Equal(SiloStatus.Active, row.Item1.Status);
            Assert.NotEqual(etag, row.Item2);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateRow_MissingRowFails()
        {
            var ct = TestContext.Current.CancellationToken;
            var table = await GetLegacyMembershipTableAsync(ct);
            var entry = CreateEntry();

            Assert.True(await table.InsertRowAsync(entry, (await table.ReadAllAsync(ct)).Version.Next(), ct));

            var etag = Assert.Single((await table.ReadRowAsync(entry.SiloAddress, ct)).Members).Item2;

            Assert.False(await table.UpdateRowAsync(CreateEntry(), etag, (await table.ReadAllAsync(ct)).Version.Next(), ct));
            Assert.Single((await table.ReadAllAsync(ct)).Members);
        }

        [Fact, TestCategory("Functional")]
        public async Task Test_UpdateIAmAlive()
        {
            var ct = TestContext.Current.CancellationToken;
            var table = await GetLegacyMembershipTableAsync(ct);
            var entry = CreateEntry();

            Assert.True(await table.InsertRowAsync(entry, (await table.ReadAllAsync(ct)).Version.Next(), ct));

            var heartbeat = entry.IAmAliveTime.AddSeconds(5);
            await table.UpdateIAmAliveAsync(new MembershipEntry { SiloAddress = entry.SiloAddress, IAmAliveTime = heartbeat }, ct);

            var row = Assert.Single((await table.ReadAllAsync(ct)).Members, x => x.Item1.SiloAddress.Equals(entry.SiloAddress));
            Assert.Equal(heartbeat, row.Item1.IAmAliveTime);
            Assert.Equal(entry.Status, row.Item1.Status);
        }

        private static MembershipEntry CreateEntry()
        {
            var now = DateTime.UtcNow;
            var time = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
            var siloAddress = SiloAddress.New(IPAddress.Loopback, 12345, Interlocked.Increment(ref generation));

            return new MembershipEntry
            {
                SiloAddress = siloAddress,
                HostName = "host",
                SiloName = $"silo-{siloAddress.Generation}",
                Status = SiloStatus.Joining,
                ProxyPort = siloAddress.Endpoint.Port,
                StartTime = time,
                IAmAliveTime = time
            };
        }
    }
}
