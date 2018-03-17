using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class NewsReminderGrain : Grain, INewsReminderGrain
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
            IGrainReminder r = null;
            r = await RegisterOrUpdateReminder(reminderName, usePeriod - TimeSpan.FromSeconds(2), usePeriod);
            return r;
        }

        /// <summary>Receieve a new Reminder.</summary>
        /// <param name="reminderName">Name of this Reminder</param>
        /// <param name="status">Status of this Reminder tick</param>
        /// <returns>Completion promise which the grain will resolve when it has finished processing this Reminder tick.</returns>
        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            Console.WriteLine("Thanks for reminding me-- I almost forgot!");
            return Task.CompletedTask;
        }
    }
}