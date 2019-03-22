using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public sealed class NewsReminderGrain : Grain, INewsReminderGrain
    {
        internal IGrainRuntime Runtime { get; set; }

        public async Task RemoveReminder(string reminder)
        {
            var reminderType = await GetReminder(reminder);

            await UnregisterReminder(reminderType);
        }

        public async Task<IGrainReminder> StartReminder(string reminderName, TimeSpan? p = null)
        {
            var usePeriod = p.Value;

            var reminder = await RegisterOrUpdateReminder(reminderName, usePeriod - TimeSpan.FromSeconds(2), usePeriod);

            return reminder;
        }
        
        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            Console.WriteLine("Thanks for reminding me-- I almost forgot!");

            return Task.CompletedTask;
        }
    }
}