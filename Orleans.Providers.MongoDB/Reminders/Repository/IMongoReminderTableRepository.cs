using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Reminders.Repository
{
    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    public interface IMongoReminderTableRepository : IDocumentRepository
    {
        Task<ReminderTableData> ReadRangeRowsKey1(string serviceId, uint beginHash, uint endHash);
        Task<ReminderTableData> ReadRangeRowsKey2(string serviceId, uint beginHash, uint endHash);

        Task<string> UpsertReminderRowAsync(
            string serviceId,
            GrainReference grainRef,
            string reminderName,
            DateTime startTime,
            TimeSpan period);
    }
}
