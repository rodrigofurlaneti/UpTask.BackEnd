using UpTask.Domain.Entities;

namespace UpTask.Domain.Interfaces
{
    public interface ITimeEntryRepository : IRepository<TimeEntry>
    {
        Task<IEnumerable<TimeEntry>> GetByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IEnumerable<TimeEntry>> GetByUserAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);
        Task<decimal> GetTotalHoursByTaskAsync(Guid taskId, CancellationToken ct = default);
    }
}
