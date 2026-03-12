using System.Threading.Tasks;
using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;
using Ts = UpTask.Domain.Enums;

namespace UpTask.Domain.Entities;

public sealed class TaskItem : Entity
{
    // ── State ─────────────────────────────────────────────────────────────────
    public Guid? ProjectId { get; private set; }
    public Guid? ParentTaskId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public TaskTitle Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public Ts.TaskStatus Status { get; private set; } = Ts.TaskStatus.Pending;
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

    // ── Navigation ────────────────────────────────────────────────────────────
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

    // ── Constructor ───────────────────────────────────────────────────────────
    private TaskItem() { } // EF Core

    // ── Factory ───────────────────────────────────────────────────────────────
    public static TaskItem Create(
        Guid createdBy,
        TaskTitle title,
        string? description,
        Priority priority,
        DateTime? dueDate,
        Guid? projectId = null,
        Guid? parentTaskId = null,
        Guid? categoryId = null,
        int? storyPoints = null,
        decimal? estimatedHours = null)
    {
        if (storyPoints.HasValue && storyPoints.Value < 0)
            throw new DomainException("Story points cannot be negative.");

        if (estimatedHours.HasValue && estimatedHours.Value < 0)
            throw new DomainException("Estimated hours cannot be negative.");

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ParentTaskId = parentTaskId,
            CreatedBy = createdBy,
            Title = title,
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            CategoryId = categoryId,
            StoryPoints = storyPoints,
            EstimatedHours = estimatedHours,
            Status = Ts.TaskStatus.Pending,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        task.RaiseDomainEvent(new TaskCreatedEvent(task.Id, createdBy, projectId));
        return task;
    }

    // ── Behavior ──────────────────────────────────────────────────────────────
    public void Update(
        TaskTitle title,
        string? description,
        Priority priority,
        DateTime? startDate,
        DateTime? dueDate,
        int? storyPoints,
        Guid? categoryId,
        decimal? estimatedHours)
    {
        if (startDate.HasValue && dueDate.HasValue && dueDate.Value < startDate.Value)
            throw new DomainException("Due date must be >= start date.");

        if (storyPoints.HasValue && storyPoints.Value < 0)
            throw new DomainException("Story points cannot be negative.");

        Title = title;
        Description = description;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        StoryPoints = storyPoints;
        CategoryId = categoryId;
        EstimatedHours = estimatedHours;
        Touch();
    }

    public void ChangeStatus(Ts.TaskStatus newStatus)
    {
        if (newStatus == Ts.TaskStatus.Completed)
            throw new DomainException("Use Complete() method to complete a task.");

        if (Status == Ts.TaskStatus.Cancelled)
            throw new DomainException("Cannot change status of a cancelled task.");

        var oldStatus = Status;
        Status = newStatus;
        Touch();
        RaiseDomainEvent(new TaskStatusChangedEvent(Id, oldStatus, newStatus));
    }

    public void Complete(Guid completedBy)
    {
        if (Status == Ts.TaskStatus.Completed)
            throw new DomainException("Task is already completed.");

        if (Status == Ts.TaskStatus.Cancelled)
            throw new DomainException("Cannot complete a cancelled task.");

        var oldStatus = Status;
        Status = Ts.TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Touch();

        RaiseDomainEvent(new TaskStatusChangedEvent(Id, oldStatus, Ts.TaskStatus.Completed));
        RaiseDomainEvent(new TaskCompletedEvent(Id, completedBy, ProjectId));
    }

    public void Assign(Guid assigneeId)
    {
        var previousAssigneeId = AssigneeId;
        AssigneeId = assigneeId;
        Touch();
        RaiseDomainEvent(new TaskAssignedEvent(Id, assigneeId, previousAssigneeId));
    }

    public void Unassign()
    {
        AssigneeId = null;
        Touch();
    }

    public void UpdateHoursWorked(decimal hours)
    {
        if (hours < 0)
            throw new DomainException("Hours worked cannot be negative.");

        HoursWorked = hours;
        Touch();
    }

    public void SetRecurrence(RecurrenceType type, DateOnly nextDate)
    {
        IsRecurring = true;
        RecurrenceType = type;
        NextRecurrence = nextDate;
        Touch();
    }

    public void RemoveRecurrence()
    {
        IsRecurring = false;
        RecurrenceType = null;
        NextRecurrence = null;
        Touch();
    }

    public void Reorder(int newOrder)
    {
        Order = newOrder;
        Touch();
    }

    // ── Domain Queries ────────────────────────────────────────────────────────
    public bool IsOverdue() =>
        DueDate.HasValue &&
        DueDate.Value < DateTime.Now &&
        Status is not (Ts.TaskStatus.Completed or Ts.TaskStatus.Cancelled);

    public bool CanBeCompletedBy(Guid userId) =>
        CreatedBy == userId || AssigneeId == userId;

    public bool CanBeEditedBy(Guid userId) =>
        CreatedBy == userId || AssigneeId == userId;
}
