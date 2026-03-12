using UpTask.Domain.Entities;
namespace UpTask.Domain.Interfaces
{
    public interface ITaskRepository : IRepository<TaskItem>
    {
        Task<IEnumerable<TaskItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
        Task<IEnumerable<TaskItem>> GetByAssigneeAsync(Guid userId, CancellationToken ct = default);
        Task<IEnumerable<TaskItem>> GetOverdueAsync(CancellationToken ct = default);
        Task<IEnumerable<TaskItem>> GetSubTasksAsync(Guid parentId, CancellationToken ct = default);
        Task<(int Total, int Completed)> GetProjectProgressAsync(Guid projectId, CancellationToken ct = default);
        Task<TaskItem?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    }
}
