using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpTask.Domain.Entities;
using UpTask.Domain.ValueObjects;

namespace UpTask.Infrastructure.Persistence.Configurations;

// ── User ──────────────────────────────────────────────────────────────────────
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);

        // Email is a Value Object — persisted as a single column
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320)
            .HasConversion(
                email => email.Value,
                value => new Email(value));

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.Profile).IsRequired();
        builder.Property(u => u.Status).IsRequired();
        builder.Property(u => u.TimeZone).IsRequired().HasMaxLength(100).HasDefaultValue("America/Sao_Paulo");
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.Phone).HasMaxLength(30);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(200);

        builder.HasMany(u => u.ProjectMemberships)
            .WithOne(pm => pm.User)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ── Project ───────────────────────────────────────────────────────────────────
internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        // ProjectName Value Object
        builder.Property(p => p.Name)
            .IsRequired().HasMaxLength(150)
            .HasConversion(n => n.Value, v => new ProjectName(v));

        // HexColor Value Object
        builder.Property(p => p.Color)
            .IsRequired().HasMaxLength(10)
            .HasConversion(c => c.Value, v => new HexColor(v));

        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Icon).HasMaxLength(100);
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.Priority).IsRequired();
        builder.Property(p => p.Progress).IsRequired().HasDefaultValue(0);

        builder.HasMany(p => p.Members)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

// ── ProjectMember ─────────────────────────────────────────────────────────────
internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.HasKey(pm => pm.Id);
        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

        builder.Property(pm => pm.Role).IsRequired();
    }
}

// ── TaskItem ──────────────────────────────────────────────────────────────────
internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        // TaskTitle Value Object
        builder.Property(t => t.Title)
            .IsRequired().HasMaxLength(250)
            .HasConversion(title => title.Value, v => new TaskTitle(v));

        builder.Property(t => t.Description).HasMaxLength(5000);
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.Priority).IsRequired();
        builder.Property(t => t.HoursWorked).HasPrecision(10, 2).HasDefaultValue(0);
        builder.Property(t => t.EstimatedHours).HasPrecision(10, 2);

        // Self-referencing for subtasks
        builder.HasMany(t => t.SubTasks)
            .WithOne(t => t.ParentTask)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Checklists)
            .WithOne()
            .HasForeignKey(cl => cl.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Tags)
            .WithOne()
            .HasForeignKey(tt => tt.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Dependencies)
            .WithOne()
            .HasForeignKey(td => td.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

// ── Comment ───────────────────────────────────────────────────────────────────
internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired().HasMaxLength(5000);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filter out soft-deleted comments at the query level
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

// ── Checklist + ChecklistItem ─────────────────────────────────────────────────
internal sealed class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("Checklists");
        builder.HasKey(cl => cl.Id);
        builder.Property(cl => cl.Title).IsRequired().HasMaxLength(200);

        builder.HasMany(cl => cl.Items)
            .WithOne()
            .HasForeignKey(i => i.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("ChecklistItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Description).IsRequired().HasMaxLength(500);
    }
}

// ── Tag + TaskTag ─────────────────────────────────────────────────────────────
internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        // CORREÇÃO: Removido .HasConversion pois Tag.Color é string
        builder.Property(t => t.Color)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
    }
}

internal sealed class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> builder)
    {
        builder.ToTable("TaskTags");
        builder.HasKey(tt => new { tt.TaskId, tt.TagId });

        builder.HasOne(tt => tt.Tag)
            .WithMany()
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ── TimeEntry ─────────────────────────────────────────────────────────────────
internal sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("TimeEntries");
        builder.HasKey(te => te.Id);
        builder.Property(te => te.Description).HasMaxLength(500);
        builder.Property(te => te.DurationMinutes).IsRequired();

        builder.HasOne(te => te.Task)
            .WithMany()
            .HasForeignKey(te => te.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(te => te.User)
            .WithMany()
            .HasForeignKey(te => te.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ── Category ──────────────────────────────────────────────────────────────────
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);

        // CORREÇÃO: Removido .HasConversion pois Category.Color é string
        builder.Property(c => c.Color)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Icon).HasMaxLength(100);
    }
}

// ── Notification ──────────────────────────────────────────────────────────────
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).HasMaxLength(1000);
        builder.Property(n => n.ReferenceType).HasMaxLength(100);
        builder.Property(n => n.Type).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}

// ── TaskDependency ────────────────────────────────────────────────────────────
internal sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies");
        builder.HasKey(td => td.Id);
        builder.HasIndex(td => new { td.TaskId, td.DependsOnId }).IsUnique();
        builder.Property(td => td.Type).IsRequired();
    }
}
