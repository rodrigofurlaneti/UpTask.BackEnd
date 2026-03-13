using Microsoft.EntityFrameworkCore;
using UpTask.Domain.Common;
using UpTask.Domain.Entities;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects;
using UpTask.Infrastructure.Persistence;
using TaskStatus = UpTask.Domain.Enums.TaskStatus;

namespace UpTask.Infrastructure.Persistence.Repositories;

// ── Generic Repository ────────────────────────────────────────────────────────
internal abstract class Repository<T>(AppDbContext context) : IRepository<T>
    where T : UpTask.Domain.Common.Entity
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
        await DbSet.AddAsync(entity, ct);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Remove(T entity) => DbSet.Remove(entity);
}

// ── Unit of Work ──────────────────────────────────────────────────────────────
internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}

// ── User Repository ───────────────────────────────────────────────────────────
internal sealed class UserRepository(AppDbContext context)
    : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailValue = email.ToLowerInvariant();
        var emailVo = new Email(emailValue);
        return await DbSet.FirstOrDefaultAsync(u => u.Email == emailVo, ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var emailValue = email.ToLowerInvariant();
        var emailVo = new Email(emailValue);
        return await DbSet.AnyAsync(u => u.Email == emailVo, ct);
    }

    public async Task<User?> GetWithSettingsAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.Id == id, ct);
}

// ── Project Repository ────────────────────────────────────────────────────────
internal sealed class ProjectRepository(AppDbContext context)
    : Repository<Project>(context), IProjectRepository
{
    public async Task<IEnumerable<Project>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await DbSet
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Project>> GetByMemberAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Include(p => p.Members)
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);

    public async Task<Project?> GetWithMembersAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project?> GetWithTasksAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
}

// ── Task Repository ───────────────────────────────────────────────────────────
internal sealed class TaskRepository(AppDbContext context)
    : Repository<TaskItem>(context), ITaskRepository
{
    public async Task<IEnumerable<TaskItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        await DbSet
            .Where(t => t.ProjectId == projectId && t.ParentTaskId == null)
            .Include(t => t.Assignee)
            .OrderBy(t => t.Order)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<TaskItem>> GetByAssigneeAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Where(t => t.AssigneeId == userId)
            .Include(t => t.Project)
            .OrderByDescending(t => t.DueDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<TaskItem>> GetOverdueAsync(CancellationToken ct = default) =>
        await DbSet
            .Where(t => t.DueDate < DateTime.UtcNow
                     && t.Status != TaskStatus.Completed
                     && t.Status != TaskStatus.Cancelled)
            .Include(t => t.Assignee)
            .ToListAsync(ct);

    public async Task<IEnumerable<TaskItem>> GetSubTasksAsync(Guid parentId, CancellationToken ct = default) =>
        await DbSet
            .Where(t => t.ParentTaskId == parentId)
            .Include(t => t.Assignee)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);

    public async Task<(int Total, int Completed)> GetProjectProgressAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var stats = await DbSet
            .Where(t => t.ProjectId == projectId && t.ParentTaskId == null)
            .GroupBy(_ => true)
            .Select(g => new
            {
                Total = g.Count(),
                Completed = g.Count(t => t.Status == TaskStatus.Completed)
            })
            .FirstOrDefaultAsync(ct);

        return stats is null ? (0, 0) : (stats.Total, stats.Completed);
    }

    public async Task<TaskItem?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(t => t.Assignee)
            .Include(t => t.Project)
            .Include(t => t.Checklists)
                .ThenInclude(cl => cl.Items)
            .Include(t => t.Tags)
                .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
}

// ── Category Repository ───────────────────────────────────────────────────────
internal sealed class CategoryRepository(AppDbContext context)
    : Repository<Category>(context), ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetGlobalAsync(CancellationToken ct = default) =>
        await DbSet.Where(c => c.UserId == null).ToListAsync(ct);

    public async Task<IEnumerable<Category>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet.Where(c => c.UserId == userId || c.UserId == null).ToListAsync(ct);
}

// ── Tag Repository ────────────────────────────────────────────────────────────
internal sealed class TagRepository(AppDbContext context)
    : Repository<Tag>(context), ITagRepository
{
    public async Task<IEnumerable<Tag>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet.Where(t => t.UserId == userId).ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid userId, string name, CancellationToken ct = default) =>
        await DbSet.AnyAsync(t => t.UserId == userId && t.Name == name.ToLowerInvariant(), ct);
}

// ── Comment Repository ────────────────────────────────────────────────────────
internal sealed class CommentRepository(AppDbContext context)
    : Repository<Comment>(context), ICommentRepository
{
    public async Task<IEnumerable<Comment>> GetByTaskAsync(Guid taskId, CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Author)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
}

// ── TimeEntry Repository ──────────────────────────────────────────────────────
internal sealed class TimeEntryRepository(AppDbContext context)
    : Repository<TimeEntry>(context), ITimeEntryRepository
{
    public async Task<IEnumerable<TimeEntry>> GetByTaskAsync(Guid taskId, CancellationToken ct = default) =>
        await DbSet.Where(te => te.TaskId == taskId).OrderByDescending(te => te.StartTime).ToListAsync(ct);

    public async Task<IEnumerable<TimeEntry>> GetByUserAsync(
        Guid userId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await DbSet
            .Where(te => te.UserId == userId && te.StartTime >= from && te.EndTime <= to)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(ct);

    public async Task<decimal> GetTotalHoursByTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var totalMinutes = await DbSet
            .Where(te => te.TaskId == taskId)
            .SumAsync(te => te.DurationMinutes, ct);

        return Math.Round(totalMinutes / 60m, 2);
    }
}

// ── Notification Repository ───────────────────────────────────────────────────
internal sealed class NotificationRepository(AppDbContext context)
    : Repository<Notification>(context), INotificationRepository
{
    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Where(n => n.UserId == userId && !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.IsRead, true)
                       .SetProperty(n => n.ReadAt, DateTime.UtcNow),
                ct);
}
