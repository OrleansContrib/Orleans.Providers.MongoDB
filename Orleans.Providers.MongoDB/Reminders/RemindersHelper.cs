using System;
using System.Collections.Generic;
using Orleans.Providers.MongoDB.Reminders.Store;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Reminders
{
    public class RemindersHelper
    {
        public static ReminderTableData ProcessRemindersList(List<MongoReminderDocument> reminders, 
            IGrainReferenceConverter grainReferenceConverter)
        {
            var reminderEntryList = new List<ReminderEntry>();

            foreach (var reminder in reminders)
            {
                reminderEntryList.Add(Parse(reminder, grainReferenceConverter));
            }

            return new ReminderTableData(reminderEntryList);
        }

        public static ReminderEntry Parse(MongoReminderDocument reminder,
            IGrainReferenceConverter grainReferenceConverter)
        {
            var grainId = reminder?.GrainId;

            if (!string.IsNullOrEmpty(grainId))
            {
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