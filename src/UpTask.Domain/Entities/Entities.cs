using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities;

// ── Category ─────────────────────────────────────────────────────────────────
public sealed class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Color { get; private set; } = "#607D8B";
    public string? Icon { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public Guid? UserId { get; private set; } // null = global

    private Category() { }

    public static Category Create(string name, string? description, string color,
        string? icon, Guid? userId, Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Category name is required.");
        return new Category
        {
            Id = Guid.NewGuid(), Name = name.Trim(), Description = description,
            Color = color, Icon = icon, UserId = userId, ParentCategoryId = parentId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, string color, string? icon)
    {
        Name = name.Trim(); Description = description; Color = color; Icon = icon;
        SetUpdatedAt();
    }

    public bool IsGlobal => UserId == null;
}

// ── Tag ──────────────────────────────────────────────────────────────────────
public sealed class Tag : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#9E9E9E";

    private Tag() { }

    public static Tag Create(Guid userId, string name, string color)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Tag name is required.");
        return new Tag
        {
            Id = Guid.NewGuid(), UserId = userId,
            Name = name.Trim().ToLowerInvariant(), Color = color,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}

// ── TaskTag (join) ────────────────────────────────────────────────────────────
public sealed class TaskTag
{
    public Guid TaskId { get; private set; }
    public Guid TagId { get; private set; }
    public Tag? Tag { get; private set; }

    private TaskTag() { }
    public static TaskTag Create(Guid taskId, Guid tagId) => new() { TaskId = taskId, TagId = tagId };
}

// ── Comment ───────────────────────────────────────────────────────────────────
public sealed class Comment : BaseEntity
{
    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsEdited { get; private set; } = false;
    public DateTime? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; } = false;
    public DateTime? DeletedAt { get; private set; }
    public User? Author { get; private set; }

    private Comment() { }

    public static Comment Create(Guid taskId, Guid userId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new BusinessRuleException("Comment content is required.");
        return new Comment
        {
            Id = Guid.NewGuid(), TaskId = taskId, UserId = userId,
            Content = content.Trim(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void Edit(string newContent)
    {
        if (IsDeleted) throw new BusinessRuleException("Cannot edit a deleted comment.");
        Content = newContent.Trim(); IsEdited = true; EditedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void SoftDelete()
    {
        IsDeleted = true; DeletedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}

// ── Checklist ─────────────────────────────────────────────────────────────────
public sealed class Checklist : BaseEntity
{
    public Guid TaskId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int Order { get; private set; } = 0;
    private readonly List<ChecklistItem> _items = [];
    public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

    private Checklist() { }

    public static Checklist Create(Guid taskId, string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new BusinessRuleException("Checklist title is required.");
        return new Checklist
        {
            Id = Guid.NewGuid(), TaskId = taskId, Title = title.Trim(),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public ChecklistItem AddItem(string description)
    {
        var item = ChecklistItem.Create(Id, description, _items.Count);
        _items.Add(item);
        SetUpdatedAt();
        return item;
    }

    public int CompletionPercentage()
    {
        if (!_items.Any()) return 0;
        return (int)Math.Floor((double)_items.Count(i => i.IsCompleted) / _items.Count * 100);
    }
}

// ── ChecklistItem ─────────────────────────────────────────────────────────────
public sealed class ChecklistItem : BaseEntity
{
    public Guid ChecklistId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; } = false;
    public Guid? CompletedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int Order { get; private set; }

    private ChecklistItem() { }

    public static ChecklistItem Create(Guid checklistId, string description, int order)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new BusinessRuleException("Item description is required.");
        return new ChecklistItem
        {
            Id = Guid.NewGuid(), ChecklistId = checklistId,
            Description = description.Trim(), Order = order,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void Complete(Guid userId)
    {
        IsCompleted = true; CompletedBy = userId; CompletedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Uncomplete()
    {
        IsCompleted = false; CompletedBy = null; CompletedAt = null;
        SetUpdatedAt();
    }
}

// ── TaskDependency ────────────────────────────────────────────────────────────
public sealed class TaskDependency : BaseEntity
{
    public Guid TaskId { get; private set; }
    public Guid DependsOnId { get; private set; }
    public DependencyType Type { get; private set; }

    private TaskDependency() { }

    public static TaskDependency Create(Guid taskId, Guid dependsOnId, DependencyType type)
    {
        if (taskId == dependsOnId) throw new BusinessRuleException("A task cannot depend on itself.");
        return new TaskDependency
        {
            Id = Guid.NewGuid(), TaskId = taskId, DependsOnId = dependsOnId, Type = type,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}

// ── TimeEntry ─────────────────────────────────────────────────────────────────
public sealed class TimeEntry : BaseEntity
{
    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Description { get; private set; }
    public TaskItem? Task { get; private set; }
    public User? User { get; private set; }

    private TimeEntry() { }

    public static TimeEntry Create(Guid taskId, Guid userId, DateTime start, DateTime end, string? description)
    {
        if (end <= start) throw new BusinessRuleException("End time must be greater than start time.");
        var duration = (int)(end - start).TotalMinutes;
        return new TimeEntry
        {
            Id = Guid.NewGuid(), TaskId = taskId, UserId = userId,
            StartTime = start, EndTime = end, DurationMinutes = duration,
            Description = description, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}

// ── Notification ──────────────────────────────────────────────────────────────
public sealed class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Message { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public bool IsRead { get; private set; } = false;
    public DateTime? ReadAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private Notification() { }

    public static Notification Create(Guid userId, NotificationType type, string title,
        string? message = null, string? referenceType = null, Guid? referenceId = null)
        => new()
        {
            Id = Guid.NewGuid(), UserId = userId, Type = type, Title = title,
            Message = message, ReferenceType = referenceType, ReferenceId = referenceId,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

    public void MarkAsRead()
    {
        IsRead = true; ReadAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
