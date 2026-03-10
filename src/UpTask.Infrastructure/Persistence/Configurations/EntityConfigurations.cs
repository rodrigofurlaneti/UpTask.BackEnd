using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;

namespace UpTask.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(u => u.Id);
        b.Property(u => u.Name).IsRequired().HasMaxLength(100);
        b.OwnsOne(u => u.Email, e => {
            e.Property(x => x.Value).HasColumnName("Email").IsRequired().HasMaxLength(150);
            e.HasIndex(x => x.Value).IsUnique();
        });
        b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
        b.Property(u => u.Profile).HasConversion<string>().HasMaxLength(20);
        b.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(u => u.TimeZone).HasMaxLength(60).HasDefaultValue("America/Sao_Paulo");
        b.HasOne(u => u.Settings).WithOne().HasForeignKey<UserSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        b.ToTable("users");
    }
}

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> b)
    {
        b.HasKey(s => s.UserId);
        b.Property(s => s.DefaultView).HasMaxLength(20).HasDefaultValue("list");
        b.Property(s => s.Theme).HasMaxLength(20).HasDefaultValue("system");
        b.Property(s => s.Language).HasMaxLength(10).HasDefaultValue("pt-BR");
        b.ToTable("user_settings");
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).IsRequired().HasMaxLength(150);
        b.Property(p => p.Color).HasMaxLength(7).HasDefaultValue("#1976D2");
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Priority).HasConversion<string>().HasMaxLength(20);
        b.HasMany(p => p.Members).WithOne(m => m.Project)
            .HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Tasks).WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable("projects");
    }
}

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> b)
    {
        b.HasKey(m => m.Id);
        b.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();
        b.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
        b.HasOne(m => m.User).WithMany(u => u.ProjectMemberships)
            .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable("project_members");
    }
}

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Title).IsRequired().HasMaxLength(250);
        b.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
        b.Property(t => t.HoursWorked).HasPrecision(6, 2).HasDefaultValue(0m);
        b.Property(t => t.EstimatedHours).HasPrecision(6, 2);
        b.HasMany(t => t.SubTasks).WithOne(s => s.ParentTask)
            .HasForeignKey(s => s.ParentTaskId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Assignee).WithMany()
            .HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(t => t.Checklists).WithOne()
            .HasForeignKey(c => c.TaskId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(t => t.Comments).WithOne()
            .HasForeignKey(c => c.TaskId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(t => t.Tags).WithOne()
            .HasForeignKey(tt => tt.TaskId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable("tasks");
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Content).IsRequired();
        b.HasOne(c => c.Author).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("comments");
    }
}

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Title).IsRequired().HasMaxLength(150);
        b.HasMany(c => c.Items).WithOne()
            .HasForeignKey(i => i.ChecklistId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable("checklists");
    }
}

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> b)
    {
        b.HasKey(i => i.Id);
        b.Property(i => i.Description).IsRequired().HasMaxLength(300);
        b.ToTable("checklist_items");
    }
}

public class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> b)
    {
        b.HasKey(tt => new { tt.TaskId, tt.TagId });
        b.HasOne(tt => tt.Tag).WithMany()
            .HasForeignKey(tt => tt.TagId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable("task_tags");
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Name).IsRequired().HasMaxLength(100);
        b.Property(c => c.Color).HasMaxLength(7).HasDefaultValue("#607D8B");
        b.ToTable("categories");
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Name).IsRequired().HasMaxLength(60);
        b.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
        b.ToTable("tags");
    }
}

public class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> b)
    {
        b.HasKey(d => d.Id);
        b.HasIndex(d => new { d.TaskId, d.DependsOnId }).IsUnique();
        b.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
        b.ToTable("task_dependencies");
    }
}

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Description).HasMaxLength(300);
        b.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Task).WithMany()
            .HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable("time_entries");
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasKey(n => n.Id);
        b.Property(n => n.Title).IsRequired().HasMaxLength(200);
        b.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        b.Property(n => n.ReferenceType).HasMaxLength(50);
        b.HasIndex(n => new { n.UserId, n.IsRead });
        b.ToTable("notifications");
    }
}
