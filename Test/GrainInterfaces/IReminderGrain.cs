using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface IReminderGrain : IGrainWithIntegerKey, IRemindable
    {
        Task<IGrainReminder> StartReminder(string reminderName, TimeSpan period);

        Task RemoveReminder(string reminder);
    }
}