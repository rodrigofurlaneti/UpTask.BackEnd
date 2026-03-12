using UpTask.Domain.Entities;
namespace UpTask.Domain.Interfaces
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default);
        Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    }
}
