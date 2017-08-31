using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    public interface INewsReminderGrain : IGrainWithIntegerKey, IRemindable
    {
        Task<IGrainReminder> StartReminder(string reminderName, TimeSpan? p = null);
        Task RemoveReminder(string reminder);
    }
}