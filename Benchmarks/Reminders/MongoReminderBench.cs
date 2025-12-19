using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NBomber.Contracts;
using NBomber.CSharp;
using Orleans.Providers.MongoDB.Reminders;

namespace Orleans.Providers.MongoDB.Benchmarks.Reminders;

public class MongoReminderBench(IHost siloHost)
{
    private readonly IGrainFactory _grainFactory = siloHost.Services.GetRequiredService<IGrainFactory>();
    
    public IEnumerable<ScenarioProps> GenerateBomberScenarios(int factor)
    {
        var rampDuration = TimeSpan.FromMinutes(1);
        var executionDuration = TimeSpan.FromMinutes(5);

        yield return Scenario.Create("long_term_growth", async context =>
            {
                // 40% is presuming long-lived reminders are being actioned and then
                var iterations = context.Random.Next() % 10 <= 4 ? 3 : 1;
                var reminderName = Guid.NewGuid().ToString();
                var step = await RepeatUpsertReminderStep(context, reminderName, iterations);

                await DeactivateGrain(step.Payload.Value);
                
                return step;
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(5 * factor, rampDuration),
                Simulation.KeepConstant(5 * factor, executionDuration)
            );

        yield return Scenario.Create("short_term_collection", async context =>
            {
                var reminderName = Guid.NewGuid().ToString();
                var createReminderStep = await RepeatUpsertReminderStep(context, reminderName, 5);

                if (!createReminderStep.Payload.IsSome()) return Response.Fail();

                var grainId = createReminderStep.Payload.Value;
                await DeleteReminderStep(context, grainId, reminderName);
                await DeactivateGrain(grainId);
                
                return Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(5 * factor, rampDuration),
                Simulation.KeepConstant(5 * factor, executionDuration)
            );

        yield return Scenario.Create("silo_shard_reading", async context =>
            {
                var reminderTable = siloHost.Services.GetRequiredService<IReminderTable>();
                await RandomRangeSelectionStep(reminderTable, context, factor);
                return Response.Ok();
            })
            .WithLoadSimulations(
                // note: reading from the shard ranges will only occur on a silo boot, which would
                // generally be an infrequent operation (even on k8s)
                Simulation.InjectRandom(0, 4, TimeSpan.FromSeconds(10), executionDuration)
            );

        yield return Scenario.Create("high_cardinality", async context =>
            {
                var numberOfReminders = context.Random.Next(5, 20);
                var createReminderStep = await CreateManyRemindersStep(context, numberOfReminders);

                if (createReminderStep.Payload.IsSome())
                {
                    var grainId = createReminderStep.Payload.Value;
                    await ReadRemindersForGrainStep(context, grainId);
                    await DeactivateGrain(grainId);
                }

                return Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.RampingConstant(5 * factor, rampDuration),
                Simulation.KeepConstant(1 * factor, executionDuration)
            );
    }

    private Task DeactivateGrain(GrainId grainId)
    {
        var grain = _grainFactory.GetGrain<IReminderBenchGrain>(grainId);
        return grain.Finished();
    }

    private Task<Response<GrainId>> CreateManyRemindersStep(IScenarioContext context, int numberOfReminders)
    {
        return Step.Run(nameof(CreateManyRemindersStep), context, async () =>
        {
            var grain = _grainFactory.GetGrain<IReminderBenchGrain>(Guid.NewGuid());
        
            foreach (var _ in Enumerable.Range(1, numberOfReminders - 1))
            {
                await grain.DoRegisterOrUpdateReminder(Guid.NewGuid().ToString());
            }

            await grain.DoRegisterOrUpdateReminder(Guid.NewGuid().ToString());
            return Response.Ok(grain.GetGrainId());
        });
    }

    private Task<Response<GrainId>> RepeatUpsertReminderStep(IScenarioContext context, string reminderName, int iterations)
    {
        return Step.Run(nameof(RepeatUpsertReminderStep), context, async () =>
        {
            var grain = _grainFactory.GetGrain<IReminderBenchGrain>(Guid.NewGuid());
            
            foreach (var _ in Enumerable.Range(1, iterations))
            {
                await grain.DoRegisterOrUpdateReminder(reminderName);
            }

            return Response.Ok(grain.GetGrainId());
        });
    }

    private Task<Response<object>> DeleteReminderStep(IScenarioContext context, GrainId grainId, string reminderName)
    {
        return Step.Run(nameof(DeleteReminderStep), context, async () =>
        {
            var grain = _grainFactory.GetGrain<IReminderBenchGrain>(grainId);
            await grain.DoDeleteReminder(reminderName);
            
            return Response.Ok();
        });
    }

    private Task<Response<object>> RandomRangeSelectionStep(IReminderTable reminderTable, IScenarioContext context, int factor)
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
            var grain = _grainFactory.GetGrain<IReminderBenchGrain>(grainId);
            await grain.DoGetReminders();
            return Response.Ok();
        });
    }

    private Task<Response<object>> ReadReminderByNameStep(IScenarioContext context, GrainId grainId, string reminderName)
    {
        return Step.Run(nameof(ReadReminderByNameStep), context, async () =>
        {
            var grain = _grainFactory.GetGrain<IReminderBenchGrain>(grainId);
            await grain.DoGetReminder(reminderName);
            return Response.Ok();
        });
    }
}

public interface IReminderBenchGrain : IGrainWithGuidKey
{
    Task DoRegisterOrUpdateReminder(string reminderName);
    Task DoGetReminders();
    Task DoGetReminder(string reminderName);
    Task DoDeleteReminder(string reminderName);

    Task Finished();
}

public class ReminderBenchGrain : Grain, IReminderBenchGrain, IRemindable
{
    public Task ReceiveReminder(string reminderName, TickStatus status)
    {
        return Task.CompletedTask;
    }

    public Task DoRegisterOrUpdateReminder(string reminderName)
    {
        var r = Random.Shared.Next(2, 30);
        return this.RegisterOrUpdateReminder(reminderName, TimeSpan.FromHours(r), TimeSpan.FromHours(r));
    }

    public Task DoGetReminders()
    {
        return this.GetReminders();
    }

    public Task DoGetReminder(string reminderName)
    {
        return this.GetReminder(reminderName);
    }

    public async Task DoDeleteReminder(string reminderName)
    {
        var reminder = await this.GetReminder(reminderName);
        if (reminder != null) await this.UnregisterReminder(reminder);
    }

    public Task Finished()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }
}
