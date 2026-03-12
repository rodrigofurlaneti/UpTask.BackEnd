using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;

namespace UpTask.Domain.Entities;
public sealed class Project : Entity
{
    // ── State ─────────────────────────────────────────────────────────────────
    public Guid OwnerId { get; private set; }
    public ProjectName Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public HexColor Color { get; private set; } = HexColor.ProjectDefault;
    public string? Icon { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Draft;
    public Priority Priority { get; private set; } = Priority.Medium;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? PlannedEndDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public int Progress { get; private set; } = 0;
    public Guid? CategoryId { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    private readonly List<ProjectMember> _members = [];
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private readonly List<TaskItem> _tasks = [];
    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

    // ── Constructor ───────────────────────────────────────────────────────────
    private Project() { } // EF Core

    // ── Factory ───────────────────────────────────────────────────────────────
    public static Project Create(
        Guid ownerId,
        ProjectName name,
        string? description,
        Priority priority,
        DateOnly? startDate,
        DateOnly? plannedEndDate,
        Guid? categoryId = null,
        HexColor? color = null)
    {
        EnsureDatesAreCoherent(startDate, plannedEndDate);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            Description = description,
            Priority = priority,
            StartDate = startDate,
            PlannedEndDate = plannedEndDate,
            CategoryId = categoryId,
            Color = color ?? HexColor.ProjectDefault,
            Status = ProjectStatus.Draft,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Business rule: owner is always an admin member
        project._members.Add(ProjectMember.Create(project.Id, ownerId, MemberRole.Admin, invitedBy: null));
        project.RaiseDomainEvent(new ProjectCreatedEvent(project.Id, ownerId));

        return project;
    }

    // ── Behavior ──────────────────────────────────────────────────────────────
    public void Update(
        ProjectName name,
        string? description,
        Priority priority,
        DateOnly? startDate,
        DateOnly? plannedEndDate,
        HexColor color,
        string? icon)
    {
        EnsureDatesAreCoherent(startDate, plannedEndDate);

        Name = name;
        Description = description;
        Priority = priority;
        StartDate = startDate;
        PlannedEndDate = plannedEndDate;
        Color = color;
        Icon = icon;
        Touch();
    }

    public void UpdateProgress(int totalTasks, int completedTasks)
    {
        Progress = totalTasks == 0
            ? 0
            : (int)Math.Floor((double)completedTasks / totalTasks * 100);

        if (Progress == 100 && Status == ProjectStatus.Active)
        {
            Status = ProjectStatus.Completed;
            ActualEndDate = DateOnly.FromDateTime(DateTime.Now);
            RaiseDomainEvent(new ProjectCompletedEvent(Id, DateTime.Now));
        }

        Touch();
    }

    public void ChangeStatus(ProjectStatus newStatus)
    {
        var oldStatus = Status;

        if (Status == ProjectStatus.Cancelled)
            throw new DomainException("Cannot change status of a cancelled project.");

        if (newStatus == Status)
            throw new DomainException($"Project is already in '{newStatus}' status.");

        Status = newStatus;
        RaiseDomainEvent(new ProjectStatusChangedEvent(Id, oldStatus, newStatus));
        Touch();
    }

    public void AddMember(Guid userId, MemberRole role, Guid invitedBy)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this project.");

        _members.Add(ProjectMember.Create(Id, userId, role, invitedBy));
        RaiseDomainEvent(new ProjectMemberAddedEvent(Id, userId, role, invitedBy));
        Touch();
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId)
            throw new DomainException("Cannot remove the project owner.");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("ProjectMember", userId);

        _members.Remove(member);
        Touch();
    }

    public void ChangeMemberRole(Guid userId, MemberRole newRole)
    {
        if (userId == OwnerId)
            throw new DomainException("Cannot change the owner's role.");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("ProjectMember", userId);

        member.ChangeRole(newRole);
        Touch();
    }

    // ── Domain Queries ────────────────────────────────────────────────────────
    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
    public bool IsAdmin(Guid userId) => _members.Any(m => m.UserId == userId && m.Role == MemberRole.Admin);
    public bool IsOwner(Guid userId) => OwnerId == userId;

    public MemberRole? GetMemberRole(Guid userId) =>
        _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    // ── Private Guards ────────────────────────────────────────────────────────
    private static void EnsureDatesAreCoherent(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
            throw new DomainException("End date must be greater than or equal to start date.");
    }
}
