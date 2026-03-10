using Microsoft.EntityFrameworkCore;
using UpTask.Domain.Common;
using UpTask.Domain.Entities;
using UpTask.Domain.Interfaces;

namespace UpTask.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch domain events before saving
        var entitiesWithEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        var result = await base.SaveChangesAsync(ct);

        // TODO: publish domain events via MediatR after save
        return result;
    }
}
