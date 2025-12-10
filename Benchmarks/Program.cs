using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoSandbox;
using NBomber.Contracts;
using NBomber.CSharp;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Benchmarks.Reminders;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Reminders;
using Orleans.Providers.MongoDB.Utils;

namespace Orleans.Providers.MongoDB.Benchmarks;

internal static class Program
{
    static void Main(string[] args)
    {
        var parser = new Parser(settings => settings.HelpWriter = Console.Out);

        var parserResult = parser.ParseArguments<BenchCommandOptions>(args);
        
        parserResult
            .WithParsed(benchOptions =>
            {
                var databaseConnectionString = MongoRunner.Run().ConnectionString;
                var mongoClientSettings = MongoClientSettings.FromConnectionString(databaseConnectionString);
                mongoClientSettings.MaxConnectionPoolSize = benchOptions.ConcurrencyFactor * 50;
                mongoClientSettings.MaxConnecting =
                    Math.Max(benchOptions.ConcurrencyFactor / 5, mongoClientSettings.MaxConnecting);
                
                IMongoClientFactory mongoClientFactory =
                    new DefaultMongoClientFactory(new MongoClient(mongoClientSettings));

                var scenarios = new List<Func<int, IEnumerable<ScenarioProps>>>();

                if (!benchOptions.SkipReminders)
                {
                    scenarios.Add(
                        GenerateReminderScenarios(mongoClientFactory)
                    );
                }

                NBomberRunner.RegisterScenarios(
                    scenarios.SelectMany(x => x(benchOptions.ConcurrencyFactor)).ToArray()
                ).Run();
            })
            .WithNotParsed(_ =>
            {
                var helpText = HelpText.AutoBuild(parserResult, help =>
                {
                    help.AdditionalNewLineAfterOption = false;
                    return help;
                });

                Console.WriteLine(helpText);
            });
    }

    private static readonly IOptions<ClusterOptions> ClusterOptions = Options.Create(
        new ClusterOptions
        {
            ServiceId = "Orleans.Providers.MongoDB.Benchmarks",
            ClusterId = "OrleansTest",
        });

    private static Func<int, IEnumerable<ScenarioProps>> GenerateReminderScenarios(IMongoClientFactory mongoClientFactory)
    {
        var reminderOptions = Options.Create(new MongoDBRemindersOptions
        {
            CollectionPrefix = "Test_",
            DatabaseName = "OrleansTest"
        });

        var nullLogger = NullLogger<MongoReminderTable>.Instance;
        var reminderTable = new MongoReminderTable(mongoClientFactory, nullLogger, reminderOptions, ClusterOptions);
        var mongoReminderBench = new MongoReminderBench(reminderTable);
        
        return mongoReminderBench.GenerateBomberScenarios;
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Local
    // ReSharper disable once ClassNeverInstantiated.Local
    private class BenchCommandOptions
    {
        [Option(shortName: 'c', longName: "concurrency-factor", Required = true)]
        public int ConcurrencyFactor { get; init; } = 1;
        
        [Option(longName: "skip-reminders", Default = false)]
        public bool SkipReminders { get; init; } = false;
    }
}