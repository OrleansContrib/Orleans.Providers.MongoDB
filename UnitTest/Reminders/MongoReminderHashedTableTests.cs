using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Reminders;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using TestExtensions;
using UnitTests;
using UnitTests.RemindersTest;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Reminders
{
    [TestCategory("Reminders")]
    [TestCategory("Mongo")]
    public class MongoReminderHashedTableTests : ReminderTableTestsBase
    {
        public MongoReminderHashedTableTests(ConnectionStringFixture fixture, TestEnvironmentFixture clusterFixture)
            : base(fixture, clusterFixture, new LoggerFilterOptions())
        {
        }

        protected override IReminderTable CreateRemindersTable()
        {
            var options = Options.Create(new MongoDBRemindersOptions
            {
                CollectionPrefix = "TestHashed_",
                DatabaseName = "OrleansTest",
                Strategy = MongoDBReminderStrategy.HashedLookupStorage
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
    }
}
