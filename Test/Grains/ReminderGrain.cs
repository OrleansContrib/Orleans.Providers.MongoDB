using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class ReminderGrain : Grain, IReminderGrain
    {
        public async Task RemoveReminder(string reminder)
        {
            var reminderType = await GetReminder(reminder);

            await UnregisterReminder(reminderType);
        }

        public async Task<IGrainReminder> StartReminder(string reminderName, TimeSpan period)
        {
            var reminder = await RegisterOrUpdateReminder(reminderName, period - TimeSpan.FromSeconds(2), period);

            return reminder;
        }
        
        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            return Task.CompletedTask;
        }
    }
}