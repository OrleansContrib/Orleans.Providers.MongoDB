using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Reminders;
using Orleans.Providers.MongoDB.Reminders.Store;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using TestExtensions;
using UnitTests;
using UnitTests.RemindersTest;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Reminders
{
    [TestCategory("Reminders")]
    [TestCategory("Mongo")]
    public class MongoReminderTableTests : ReminderTableTestsBase
    {
        public MongoReminderTableTests(ConnectionStringFixture fixture, TestEnvironmentFixture clusterFixture)
            : base(fixture, clusterFixture, new LoggerFilterOptions())
        {
        }

        protected override IReminderTable CreateRemindersTable()
        {
            var options = Options.Create(new MongoDBRemindersOptions
            {
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest"
            });

            return new MongoReminderTable(
                MongoDatabaseFixture.DatabaseFactory,
                loggerFactory.CreateLogger<MongoReminderTable>(),
                options,
                clusterOptions);
        }

        protected override Task<string> GetConnectionString()
        {
            return Task.FromResult(MongoDatabaseFixture.DatabaseConnectionString);
        }

        [Fact]
        public async Task Test_RemindersRange()
        {
            await RemindersRange(50);
        }

        [Fact]
        public async Task Test_RemindersParallelUpsert()
        {
            await RemindersParallelUpsert();
        }

        [Fact]
        public async Task Test_ReminderSimple()
        {
            await ReminderSimple();
        }

        [Fact]
        public async Task CreateIndexesFalse_DoesNotCreateReminderIndexes()
        {
            var collectionPrefix = $"NoIndexes_{System.Guid.NewGuid():N}_";
            var collection = new MongoReminderCollection(
                new MongoClient(MongoDatabaseFixture.DatabaseConnectionString),
                "OrleansTest",
                collectionPrefix,
                null,
                false,
                false,
                "TestService",
                false);

            await collection.ReadRows(0, uint.MaxValue);

            var indexes = await collection.Client
                .GetDatabase("OrleansTest")
                .GetCollection<MongoReminderDocument>($"{collectionPrefix}OrleansReminderV2")
                .Indexes.ListAsync(TestContext.Current.CancellationToken);

            Assert.DoesNotContain(await indexes.ToListAsync(TestContext.Current.CancellationToken), index => index["name"] == "ByGrainHash");
        }

        [Fact]
        public async Task CreateIndexesTrue_CreatesReminderIndexes()
        {
            var collectionPrefix = $"WithIndexes_{System.Guid.NewGuid():N}_";
            var collection = new MongoReminderCollection(
                new MongoClient(MongoDatabaseFixture.DatabaseConnectionString),
                "OrleansTest",
                collectionPrefix,
                null,
                false,
                false,
                "TestService",
                true);

            await collection.ReadRows(0, uint.MaxValue);

            var indexes = await collection.Client
                .GetDatabase("OrleansTest")
                .GetCollection<MongoReminderDocument>($"{collectionPrefix}OrleansReminderV2")
                .Indexes.ListAsync(TestContext.Current.CancellationToken);

            Assert.Contains(await indexes.ToListAsync(TestContext.Current.CancellationToken), index => index["name"] == "ByGrainHash");
        }
    }
}
