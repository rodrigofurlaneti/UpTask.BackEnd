using UpTask.Domain.Entities;
namespace UpTask.Domain.Interfaces
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<IEnumerable<Project>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
        Task<IEnumerable<Project>> GetByMemberAsync(Guid userId, CancellationToken ct = default);
        Task<Project?> GetWithMembersAsync(Guid id, CancellationToken ct = default);
        Task<Project?> GetWithTasksAsync(Guid id, CancellationToken ct = default);
    }
}
