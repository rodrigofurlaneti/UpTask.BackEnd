using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities;

public sealed class Project : BaseEntity
{
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Color { get; private set; } = "#1976D2";
    public string? Icon { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Draft;
    public Priority Priority { get; private set; } = Priority.Medium;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? PlannedEndDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public int Progress { get; private set; } = 0;
    public Guid? CategoryId { get; private set; }

    private readonly List<ProjectMember> _members = [];
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private readonly List<TaskItem> _tasks = [];
    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

    private Project() { }

    public static Project Create(Guid ownerId, string name, string? description,
        Priority priority, DateOnly? startDate, DateOnly? plannedEndDate, Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Project name is required.");
        if (startDate.HasValue && plannedEndDate.HasValue && plannedEndDate < startDate)
            throw new BusinessRuleException("End date must be greater than or equal to start date.");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name.Trim(),
            Description = description,
            Priority = priority,
            StartDate = startDate,
            PlannedEndDate = plannedEndDate,
            CategoryId = categoryId,
            Status = ProjectStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Owner is automatically an Admin member
        project._members.Add(ProjectMember.Create(project.Id, ownerId, MemberRole.Admin, null));
        project.AddDomainEvent(new ProjectCreatedEvent(project.Id, ownerId));
        return project;
    }

    public void Update(string name, string? description, Priority priority,
        DateOnly? startDate, DateOnly? plannedEndDate, string color, string? icon)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Project name is required.");
        if (startDate.HasValue && plannedEndDate.HasValue && plannedEndDate < startDate)
            throw new BusinessRuleException("End date must be >= start date.");

        Name = name.Trim();
        Description = description;
        Priority = priority;
        StartDate = startDate;
        PlannedEndDate = plannedEndDate;
        Color = color;
        Icon = icon;
        SetUpdatedAt();
    }

    public void UpdateProgress(int totalTasks, int completedTasks)
    {
        Progress = totalTasks == 0 ? 0 : (int)Math.Floor((double)completedTasks / totalTasks * 100);
        if (Progress == 100 && Status == ProjectStatus.Active)
        {
            Status = ProjectStatus.Completed;
            ActualEndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            AddDomainEvent(new ProjectCompletedEvent(Id));
        }
        SetUpdatedAt();
    }

    public void ChangeStatus(ProjectStatus newStatus)
    {
        Status = newStatus;
        SetUpdatedAt();
    }

    public void AddMember(Guid userId, MemberRole role, Guid invitedBy)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new BusinessRuleException("User is already a member of this project.");
        _members.Add(ProjectMember.Create(Id, userId, role, invitedBy));
        SetUpdatedAt();
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId) throw new BusinessRuleException("Cannot remove the project owner.");
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("ProjectMember", userId);
        _members.Remove(member);
        SetUpdatedAt();
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
    public bool IsAdmin(Guid userId) => _members.Any(m => m.UserId == userId && m.Role == MemberRole.Admin);
}
