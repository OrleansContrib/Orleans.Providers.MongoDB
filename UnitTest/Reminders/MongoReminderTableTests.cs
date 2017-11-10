using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Reminders;
using Orleans.Runtime;
using TestExtensions;
using UnitTests;
using UnitTests.RemindersTest;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Reminders
{
    [TestCategory("Reminders")]
    [TestCategory("Mongo")]
    [Collection(TestEnvironmentFixture.DefaultCollection)]
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
                ConnectionString = "mongodb://localhost/OrleansTest",
                CollectionPrefix = "Test_",
                DatabaseName = "OrleansTest"
            });

            return new MongoReminderTable(loggerFactory.CreateLogger<MongoReminderTable>(), options, 
                ClusterFixture.Client.ServiceProvider.GetRequiredService<IGrainReferenceConverter>(),
                null);
        }

        protected override Task<string> GetConnectionString()
        {
            return Task.FromResult("mongodb://localhost/OrleansTest");
        }

        [Fact]
        public void RemindersTable_MongoDB_Init()
        {
        }

        [Fact]
        public async Task RemindersTable_MongoDB_RemindersRange()
        {
            await RemindersRange(50);
        }

        [Fact]
        public async Task RemindersTable_MongoDB_RemindersParallelUpsert()
        {
            await RemindersParallelUpsert();
        }

        [Fact]
        public async Task RemindersTable_MongoDB_ReminderSimple()
        {
            await ReminderSimple();
        }
    }
}
