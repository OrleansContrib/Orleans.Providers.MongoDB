using System.Runtime.CompilerServices;
using NBomber.Contracts;
using NBomber.CSharp;
using Orleans.Providers.MongoDB.Reminders;

namespace Orleans.Providers.MongoDB.Benchmarks.Reminders;

public class MongoReminderBench(MongoReminderTable reminderTable)
{
    public IEnumerable<ScenarioProps> GenerateBomberScenarios(int factor)
    {
        var rampDuration = TimeSpan.FromMinutes(1);
        var executionDuration = TimeSpan.FromMinutes(5);

        yield return Scenario.Create("long_term_growth", async context =>
            {
                var createReminderStep = await CreateReminderStep(context);

                if (!createReminderStep.Payload.IsSome()) return Response.Fail();
                
                var entry = createReminderStep.Payload.Value;
                await ReadReminderByNameStep(context, entry.GrainId, entry.ReminderName);

                if (context.Random.Next() % 10 <= 4)
                {
                    // 40% is presuming long-lived reminders are being actioned and then
                    // de-registered as the grain no longer has any actions
                    await UpdateReminderStep(context, entry);
                }

                return Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(5 * factor, rampDuration),
                Simulation.KeepConstant(5 * factor, executionDuration)
            )
            .WithInit(_ => reminderTable.Init());

        yield return Scenario.Create("short_term_collection", async context =>
            {
                var createReminderStep = await CreateReminderStep(context);

                if (!createReminderStep.Payload.IsSome()) return Response.Fail();

                await DeleteReminderStep(context, createReminderStep.Payload.Value);
                return Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(5 * factor, rampDuration),
                Simulation.KeepConstant(5 * factor, executionDuration)
            )
            .WithInit(_ => reminderTable.Init());

        yield return Scenario.Create("silo_shard_reading", async context =>
            {
                await RandomRangeSelectionStep(context, factor);
                return Response.Ok();
            })
            .WithLoadSimulations(
                // note: reading from the shard ranges will only occur on a silo boot, which would
                // generally be an infrequent operation (even on k8s)
                Simulation.InjectRandom(0, 4, TimeSpan.FromSeconds(10), executionDuration)
            )
            .WithInit(_ => reminderTable.Init());

        yield return Scenario.Create("high_cardinality", async context =>
            {
                var numberOfReminders = context.Random.Next(5, 20);
                var createReminderStep = await CreateReminderStep(context, numberOfReminders);
        
                if (createReminderStep.Payload.IsSome())
                {
                    await ReadRemindersForGrainStep(context, createReminderStep.Payload.Value.GrainId);
                }
        
                return Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(5 * factor, rampDuration),
                Simulation.KeepConstant(1 * factor, executionDuration)
            )
            .WithInit(_ => reminderTable.Init());
    }

    private Task<Response<ReminderEntry>> CreateReminderStep(IScenarioContext context, int numberOfReminders = 1)
    {
        return Step.Run(nameof(CreateReminderStep), context, async () =>
        {
            var grainId = GrainId.Create(GrainType.Create("foo"), IdSpan.Create(Guid.NewGuid().ToString()));
            bool anyFailures = false;
        
            foreach (var _ in Enumerable.Range(1, numberOfReminders - 1))
            {
                anyFailures |= (await CreateReminderEntry(grainId)).ETag == null;
            }

            var returnedEntry = await CreateReminderEntry(grainId);
            anyFailures |= returnedEntry.ETag == null;

            return anyFailures ? Response.Fail(returnedEntry) : Response.Ok(returnedEntry);
        });

        async Task<ReminderEntry> CreateReminderEntry(GrainId grainId)
        {
            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            
            var entry = new ReminderEntry
            {
                GrainId = grainId,
                Period = TimeSpan.FromMinutes(1),
                StartAt = now,
                ReminderName = Guid.NewGuid().ToString()
            };

            await reminderTable.UpsertRow(entry);
            return entry;
        }
    }

    private Task<Response<object>> DeleteReminderStep(IScenarioContext context, ReminderEntry entry)
    {
        return Step.Run(nameof(DeleteReminderStep), context, async () =>
        {
            var success = await reminderTable.RemoveRow(entry.GrainId, entry.ReminderName, entry.ETag);
            
            return Response.Ok();
        });
    }

    private Task<Response<object>> UpdateReminderStep(IScenarioContext context, ReminderEntry entry)
    {
        return Step.Run(nameof(UpdateReminderStep), context, async () =>
        {
            var r = context.Random.Next(2, 30);
            entry.Period = TimeSpan.FromMinutes(1) + TimeSpan.FromMinutes(r);
            entry.StartAt = entry.StartAt.Add(TimeSpan.FromHours(r));
            
            var etag = await reminderTable.UpsertRow(entry);
            if (etag == null) throw new Exception("Concurrency: Reminder entry not found");
            return Response.Ok();
        });
    }

    private Task<Response<object>> RandomRangeSelectionStep(IScenarioContext context, int factor)
    {
        return Step.Run(nameof(RandomRangeSelectionStep), context, async () =>
        {
            // we just want random bits to fake a uint random number, which has a higher max value due to no negative space
            var start = Unsafe.BitCast<int, uint>(context.Random.Next(int.MinValue, int.MaxValue));
            var increment = (uint)(int.MaxValue / factor);
            var end = unchecked(start + increment * 2);

            await reminderTable.ReadRows(start, end);
            return Response.Ok();
        });
    }

    private Task<Response<object>> ReadRemindersForGrainStep(IScenarioContext context, GrainId grainId)
    {
        return Step.Run(nameof(ReadRemindersForGrainStep), context, async () =>
        {
            await reminderTable.ReadRows(grainId);
            return Response.Ok();
        });
    }

    private Task<Response<object>> ReadReminderByNameStep(IScenarioContext context, GrainId grainId, string reminderName)
    {
        return Step.Run(nameof(ReadReminderByNameStep), context, async () =>
        {
            await reminderTable.ReadRow(grainId, reminderName);
            return Response.Ok();
        });
    }
}