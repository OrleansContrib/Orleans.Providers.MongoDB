using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Reminders
{
    public class RemindersHelper
    {
        public static Task<ReminderTableData> ProcessRemindersList(List<RemindersCollection> reminders, IGrainReferenceConverter grainReferenceConverter)
        {
            List<ReminderEntry> reminderEntryList = new List<ReminderEntry>();
            foreach (var reminder in reminders)
            {
                reminderEntryList.Add(Parse(reminder, grainReferenceConverter));
            }

            return Task.FromResult(new ReminderTableData(reminderEntryList));
        }

        public static ReminderEntry Parse(RemindersCollection reminder, IGrainReferenceConverter grainReferenceConverter)
        {
            if (reminder != null)
            {
                string grainId = reminder.GrainId;

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
            }

            return null;
        }

    }
}
