using UpTask.Domain.Entities;
using UpTask.Domain.Enums;

namespace UpTask.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<User?> GetWithSettingsAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetByMemberAsync(Guid userId, CancellationToken ct = default);
    Task<Project?> GetWithMembersAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetWithTasksAsync(Guid id, CancellationToken ct = default);
}

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<IEnumerable<TaskItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetByAssigneeAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetOverdueAsync(CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetSubTasksAsync(Guid parentId, CancellationToken ct = default);
    Task<(int Total, int Completed)> GetProjectProgressAsync(Guid projectId, CancellationToken ct = default);
    Task<TaskItem?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetGlobalAsync(CancellationToken ct = default);
    Task<IEnumerable<Category>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}

public interface ITagRepository : IRepository<Tag>
{
    Task<IEnumerable<Tag>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, string name, CancellationToken ct = default);
}

public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByTaskAsync(Guid taskId, CancellationToken ct = default);
}

public interface ITimeEntryRepository : IRepository<TimeEntry>
{
    Task<IEnumerable<TimeEntry>> GetByTaskAsync(Guid taskId, CancellationToken ct = default);
    Task<IEnumerable<TimeEntry>> GetByUserAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<decimal> GetTotalHoursByTaskAsync(Guid taskId, CancellationToken ct = default);
}

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
