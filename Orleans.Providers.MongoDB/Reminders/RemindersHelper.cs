using System;
using System.Collections.Generic;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Reminders
{
    public class RemindersHelper
    {
        public static ReminderTableData ProcessRemindersList(List<RemindersCollection> reminders,
            IGrainReferenceConverter grainReferenceConverter)
        {
            var reminderEntryList = new List<ReminderEntry>();
            foreach (var reminder in reminders)
                reminderEntryList.Add(Parse(reminder, grainReferenceConverter));

            return new ReminderTableData(reminderEntryList);
        }

        public static ReminderEntry Parse(RemindersCollection reminder,
            IGrainReferenceConverter grainReferenceConverter)
        {
            if (reminder != null)
            {
                var grainId = reminder.GrainId;

                if (!string.IsNullOrEmpty(grainId))
                    return new ReminderEntry
                    {
                        GrainRef = grainReferenceConverter.GetGrainFromKeyString(grainId),
                        ReminderName = reminder.ReminderName,
                        StartAt = reminder.StartTime,
                        Period = TimeSpan.FromMilliseconds(reminder.Period),
                        ETag = reminder.Version.ToString()
                    };
            }

            return null;
        }
    }
}