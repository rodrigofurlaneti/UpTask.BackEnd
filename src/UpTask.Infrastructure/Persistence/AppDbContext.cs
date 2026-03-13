using MediatR;
using Microsoft.EntityFrameworkCore;
using UpTask.Domain.Common;
using UpTask.Domain.Entities;
using UpTask.Domain.ValueObjects;

namespace UpTask.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext. Owns all aggregate tables.
/// Dispatches domain events as part of SaveChangesAsync via IPublisher.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher? publisher = null)
    : DbContext(options)
{
    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();
        var result = await base.SaveChangesAsync(cancellationToken);
        if (publisher is not null)
        {
            foreach (var entity in entitiesWithEvents)
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();

                foreach (var domainEvent in events)
                    await publisher.Publish(domainEvent, cancellationToken);
            }
        }
        return result;
    }
}
