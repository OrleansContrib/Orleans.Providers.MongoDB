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
    public class MongoReminderTableTests : ReminderTableTestsBase
    {
        private MongoClientJig mongoClientFixture;

        public MongoReminderTableTests(ConnectionStringFixture fixture, TestEnvironmentFixture clusterFixture)
            : base(fixture, clusterFixture, new LoggerFilterOptions())
        {
        }

        protected override IReminderTable CreateRemindersTable()
        {
            mongoClientFixture ??= new MongoClientJig();
            
            var options = Options.Create(new MongoDBRemindersOptions
            {
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest"
            });

            return new MongoReminderTable(
                mongoClientFixture.CreateDatabaseFactory(),
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
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact]
        public async Task Test_RemindersParallelUpsert()
        {
            await RemindersParallelUpsert();
            await mongoClientFixture.AssertQualityChecksAsync();
        }

        [Fact]
        public async Task Test_ReminderSimple()
        {
            await ReminderSimple();
            await mongoClientFixture.AssertQualityChecksAsync();
        }
    }
}
