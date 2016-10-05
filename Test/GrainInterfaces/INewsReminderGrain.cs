using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.GrainInterfaces
{
    using Orleans.Runtime;

    public interface INewsReminderGrain : IGrainWithIntegerKey, IRemindable
    {
        Task<IGrainReminder> StartReminder(string reminderName, TimeSpan? p = null);
    }
}
