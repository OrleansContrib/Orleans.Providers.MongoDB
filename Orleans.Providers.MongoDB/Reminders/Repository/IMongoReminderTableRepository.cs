using System;
using System.Threading.Tasks;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Reminders.Repository
{
    public interface IMongoReminderTableRepository : IDocumentRepository
    {
        Task<ReminderTableData> ReadRangeRowsKey1Async(string serviceId, uint beginHash, uint endHash);
        Task<ReminderTableData> ReadRangeRowsKey2Async(string serviceId, uint beginHash, uint endHash);
        Task<ReminderEntry> ReadReminderRowAsync(string serviceId, GrainReference grainRef, string reminderName);
        Task<ReminderTableData> ReadReminderRowAsync(string serviceId, GrainReference grainRef);

        Task<string> UpsertReminderRowAsync(
            string serviceId,
            GrainReference grainRef,
            string reminderName,
            DateTime startTime,
            TimeSpan period);

        Task<bool> RemoveRowAsync(string serviceId, GrainReference grainRef, string reminderName, string eTag);

        Task<ReminderTableData> ReadReminderRowsAsync(string serviceId, GrainReference grainRef);

        Task RemoveReminderRowsAsync(string serviceId);

        Task InitTables();
    }
}