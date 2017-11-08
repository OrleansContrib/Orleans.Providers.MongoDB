namespace Orleans.Providers.MongoDB.Test.Grains
{
    public class NewsReminderGrain : Grain, INewsReminderGrain
    {
        internal IGrainRuntime Runtime { get; set; }

        /// <summary>
        ///     Registers a persistent, reliable reminder to send regular notifications (reminders) to the grain.
        ///     The grain must implement the <c>Orleans.IRemindable</c> interface, and reminders for this grain will be sent to the
        ///     <c>ReceiveReminder</c> callback method.
        ///     If the current grain is deactivated when the timer fires, a new activation of this grain will be created to receive
        ///     this reminder.
        ///     If an existing reminder with the same name already exists, that reminder will be overwritten with this new
        ///     reminder.
        ///     Reminders will always be received by one activation of this grain, even if multiple activations exist for this
        ///     grain.
        /// </summary>
        /// <param name="reminderName">Name of this reminder</param>
        /// <param name="dueTime">Due time for this reminder</param>
        /// <param name="period">Frequence period for this reminder</param>
        /// <returns>Promise for Reminder handle.</returns>
        protected virtual Task<IGrainReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime,
            TimeSpan period)
        {
            if (!(this is IRemindable))
            {
                throw new InvalidOperationException(string.Format(
                    "Grain {0} is not 'IRemindable'. A grain should implement IRemindable to use the persistent reminder service",
                    IdentityString));
            }

            return base.RegisterOrUpdateReminder(reminderName, dueTime, period);
        }

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