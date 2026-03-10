using Microsoft.EntityFrameworkCore;
using UpTask.Domain.Entities;
using UpTask.Domain.Interfaces;

namespace UpTask.Infrastructure.Persistence.Repositories;

// ── Generic ───────────────────────────────────────────────────────────────────
public abstract class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    protected readonly AppDbContext _db = db;
    protected DbSet<T> DbSet => _db.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public void Update(T entity) => DbSet.Update(entity);
    public void Remove(T entity) => DbSet.Remove(entity);
}

// ── User ──────────────────────────────────────────────────────────────────────
public sealed class UserRepository(AppDbContext db) : Repository<User>(db), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email.Value == email.ToLowerInvariant(), ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await _db.Users.AnyAsync(u => u.Email.Value == email.ToLowerInvariant(), ct);

    public async Task<User?> GetWithSettingsAsync(Guid id, CancellationToken ct = default)
        => await _db.Users.Include(u => u.Settings).FirstOrDefaultAsync(u => u.Id == id, ct);
}

// ── Project ───────────────────────────────────────────────────────────────────
public sealed class ProjectRepository(AppDbContext db) : Repository<Project>(db), IProjectRepository
{
    public async Task<IEnumerable<Project>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        => await _db.Projects.Where(p => p.OwnerId == ownerId).ToListAsync(ct);

    public async Task<IEnumerable<Project>> GetByMemberAsync(Guid userId, CancellationToken ct = default)
        => await _db.Projects
            .Include(p => p.Members)
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);

    public async Task<Project?> GetWithMembersAsync(Guid id, CancellationToken ct = default)
        => await _db.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project?> GetWithTasksAsync(Guid id, CancellationToken ct = default)
        => await _db.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id, ct);
}

// ── Task ──────────────────────────────────────────────────────────────────────
public sealed class TaskRepository(AppDbContext db) : Repository<TaskItem>(db), ITaskRepository
{
    public async Task<IEnumerable<TaskItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _db.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.ProjectId == projectId && t.ParentTaskId == null)
            .OrderBy(t => t.Order).ToListAsync(ct);

    public async Task<IEnumerable<TaskItem>> GetByAssigneeAsync(Guid userId, CancellationToken ct = default)
        => await _db.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Project)
            .Where(t => (t.AssigneeId == userId || t.CreatedBy == userId) && t.Status != Domain.Enums.TaskStatus.Cancelled)
            .OrderBy(t => t.DueDate).ToListAsync(ct);

    public async Task<IEnumerable<TaskItem>> GetOverdueAsync(CancellationToken ct = default)
        => await _db.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.DueDate < DateTime.UtcNow
                     && t.Status != Domain.Enums.TaskStatus.Completed
                     && t.Status != Domain.Enums.TaskStatus.Cancelled)
            .ToListAsync(ct);

    public async Task<IEnumerable<TaskItem>> GetSubTasksAsync(Guid parentId, CancellationToken ct = default)
        => await _db.Tasks.Where(t => t.ParentTaskId == parentId).ToListAsync(ct);

    public async Task<(int Total, int Completed)> GetProjectProgressAsync(Guid projectId, CancellationToken ct = default)
    {
        var counts = await _db.Tasks
            .Where(t => t.ProjectId == projectId && t.ParentTaskId == null)
            .GroupBy(t => 1)
            .Select(g => new { Total = g.Count(), Completed = g.Count(t => t.Status == Domain.Enums.TaskStatus.Completed) })
            .FirstOrDefaultAsync(ct);
        return (counts?.Total ?? 0, counts?.Completed ?? 0);
    }

    public async Task<TaskItem?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _db.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Project)
            .Include(t => t.Checklists).ThenInclude(c => c.Items)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.Tags).ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
}

// ── Category ──────────────────────────────────────────────────────────────────
public sealed class CategoryRepository(AppDbContext db) : Repository<Category>(db), ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetGlobalAsync(CancellationToken ct = default)
        => await _db.Categories.Where(c => c.UserId == null).ToListAsync(ct);

    public async Task<IEnumerable<Category>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.Categories.Where(c => c.UserId == userId).ToListAsync(ct);
}

// ── Tag ───────────────────────────────────────────────────────────────────────
public sealed class TagRepository(AppDbContext db) : Repository<Tag>(db), ITagRepository
{
    public async Task<IEnumerable<Tag>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.Tags.Where(t => t.UserId == userId).OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid userId, string name, CancellationToken ct = default)
        => await _db.Tags.AnyAsync(t => t.UserId == userId && t.Name == name.ToLowerInvariant(), ct);
}

// ── Comment ───────────────────────────────────────────────────────────────────
public sealed class CommentRepository(AppDbContext db) : Repository<Comment>(db), ICommentRepository
{
    public async Task<IEnumerable<Comment>> GetByTaskAsync(Guid taskId, CancellationToken ct = default)
        => await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.TaskId == taskId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt).ToListAsync(ct);
}

// ── TimeEntry ─────────────────────────────────────────────────────────────────
public sealed class TimeEntryRepository(AppDbContext db) : Repository<TimeEntry>(db), ITimeEntryRepository
{
    public async Task<IEnumerable<TimeEntry>> GetByTaskAsync(Guid taskId, CancellationToken ct = default)
        => await _db.TimeEntries
            .Include(e => e.User)
            .Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.StartTime).ToListAsync(ct);

    public async Task<IEnumerable<TimeEntry>> GetByUserAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.TimeEntries
            .Include(e => e.Task)
            .Where(e => e.UserId == userId && e.StartTime >= from && e.StartTime <= to)
            .OrderByDescending(e => e.StartTime).ToListAsync(ct);

    public async Task<decimal> GetTotalHoursByTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var totalMin = await _db.TimeEntries
            .Where(e => e.TaskId == taskId)
            .SumAsync(e => e.DurationMinutes, ct);
        return Math.Round((decimal)totalMin / 60, 2);
    }
}

// ── Notification ──────────────────────────────────────────────────────────────
public sealed class NotificationRepository(AppDbContext db) : Repository<Notification>(db), INotificationRepository
{
    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt).ToListAsync(ct);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
        => await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
}
