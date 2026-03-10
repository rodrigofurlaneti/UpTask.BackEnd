using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using TaskStatus = UpTask.Domain.Enums.TaskStatus;

namespace UpTask.Domain.Entities;

public sealed class TaskItem : BaseEntity
{
    public Guid? ProjectId { get; private set; }
    public Guid? ParentTaskId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; } = TaskStatus.Pending;
    public Priority Priority { get; private set; } = Priority.Medium;
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public decimal? EstimatedHours { get; private set; }
    public decimal HoursWorked { get; private set; } = 0;
    public int? StoryPoints { get; private set; }
    public int Order { get; private set; } = 0;
    public bool IsRecurring { get; private set; } = false;
    public RecurrenceType? RecurrenceType { get; private set; }
    public DateOnly? NextRecurrence { get; private set; }

    // Navigation
    public Project? Project { get; private set; }
    public TaskItem? ParentTask { get; private set; }
    public User? Assignee { get; private set; }
    private readonly List<TaskItem> _subTasks = [];
    public IReadOnlyCollection<TaskItem> SubTasks => _subTasks.AsReadOnly();
    private readonly List<Comment> _comments = [];
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    private readonly List<TaskTag> _tags = [];
    public IReadOnlyCollection<TaskTag> Tags => _tags.AsReadOnly();
    private readonly List<Checklist> _checklists = [];
    public IReadOnlyCollection<Checklist> Checklists => _checklists.AsReadOnly();
    private readonly List<TaskDependency> _dependencies = [];
    public IReadOnlyCollection<TaskDependency> Dependencies => _dependencies.AsReadOnly();

    private TaskItem() { }

    public static TaskItem Create(Guid createdBy, string title, string? description,
        Priority priority, DateTime? dueDate, Guid? projectId = null, Guid? parentTaskId = null,
        Guid? categoryId = null, int? storyPoints = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new BusinessRuleException("Task title is required.");
        if (parentTaskId.HasValue && parentTaskId == Guid.Empty)
            throw new BusinessRuleException("Invalid parent task ID.");

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ParentTaskId = parentTaskId,
            CreatedBy = createdBy,
            Title = title.Trim(),
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            CategoryId = categoryId,
            StoryPoints = storyPoints,
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        task.AddDomainEvent(new TaskCreatedEvent(task.Id, createdBy, projectId));
        return task;
    }

    public void Update(string title, string? description, Priority priority,
        DateTime? startDate, DateTime? dueDate, int? storyPoints, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new BusinessRuleException("Task title is required.");
        if (startDate.HasValue && dueDate.HasValue && dueDate < startDate)
            throw new BusinessRuleException("Due date must be >= start date.");

        Title = title.Trim();
        Description = description;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        StoryPoints = storyPoints;
        CategoryId = categoryId;
        SetUpdatedAt();
    }

    public void ChangeStatus(TaskStatus newStatus)
    {
        if (newStatus == TaskStatus.Completed)
            throw new BusinessRuleException("Use Complete() method to complete a task.");

        Status = newStatus;
        SetUpdatedAt();
        AddDomainEvent(new TaskStatusChangedEvent(Id, Status, newStatus));
    }

    public void Complete(Guid completedBy)
    {
        if (Status == TaskStatus.Completed) throw new BusinessRuleException("Task is already completed.");
        if (Status == TaskStatus.Cancelled) throw new BusinessRuleException("Cannot complete a cancelled task.");

        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        SetUpdatedAt();
        AddDomainEvent(new TaskCompletedEvent(Id, completedBy, ProjectId));
    }

    public void Assign(Guid assigneeId)
    {
        AssigneeId = assigneeId;
        SetUpdatedAt();
        AddDomainEvent(new TaskAssignedEvent(Id, assigneeId));
    }

    public void UpdateHoursWorked(decimal hours)
    {
        if (hours < 0) throw new BusinessRuleException("Hours worked cannot be negative.");
        HoursWorked = hours;
        SetUpdatedAt();
    }

    public void SetRecurrence(RecurrenceType type, DateOnly nextDate)
    {
        IsRecurring = true;
        RecurrenceType = type;
        NextRecurrence = nextDate;
        SetUpdatedAt();
    }

    public bool IsOverdue() => DueDate.HasValue && DueDate < DateTime.UtcNow
        && Status is not (TaskStatus.Completed or TaskStatus.Cancelled);
}