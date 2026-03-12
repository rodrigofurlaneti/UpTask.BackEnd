using FluentAssertions;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;
using TaskStatus = UpTask.Domain.Enums.TaskStatus;
using Xunit;

namespace UpTask.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the TaskItem aggregate root.
/// Tests enforce all domain invariants defined in the business rules.
/// </summary>
public sealed class TaskItemTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static TaskItem CreateSut(
        string title = "Test Task",
        Priority priority = Priority.Medium,
        DateTime? dueDate = null,
        Guid? projectId = null) =>
        TaskItem.Create(
            createdBy: UserId,
            title: new TaskTitle(title),
            description: null,
            priority: priority,
            dueDate: dueDate,
            projectId: projectId);

    // ── Creation ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetStatusToPending()
    {
        var task = CreateSut();

        task.Status.Should().Be(TaskStatus.Pending);
    }

    [Fact]
    public void Create_WithValidData_ShouldRaiseTaskCreatedEvent()
    {
        var task = CreateSut();

        task.DomainEvents.Should().ContainSingle(e => e is TaskCreatedEvent);
        var evt = task.DomainEvents.OfType<TaskCreatedEvent>().Single();
        evt.CreatedBy.Should().Be(UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ShouldThrowDomainException(string title)
    {
        var act = () => new TaskTitle(title);

        act.Should().Throw<DomainException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_ShouldThrowDomainException()
    {
        var longTitle = new string('A', TaskTitle.MaxLength + 1);

        var act = () => new TaskTitle(longTitle);

        act.Should().Throw<DomainException>()
            .WithMessage($"*{TaskTitle.MaxLength}*");
    }

    [Fact]
    public void Create_WithNegativeStoryPoints_ShouldThrowDomainException()
    {
        var act = () => TaskItem.Create(
            UserId, new TaskTitle("Title"), null, Priority.Low, null,
            storyPoints: -1);

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_WhenPending_ShouldSetStatusToCompleted()
    {
        var task = CreateSut();

        task.Complete(UserId);

        task.Status.Should().Be(TaskStatus.Completed);
        task.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Complete_WhenPending_ShouldRaiseTaskCompletedEvent()
    {
        var task = CreateSut();
        task.ClearDomainEvents();

        task.Complete(UserId);

        task.DomainEvents.Should().ContainSingle(e => e is TaskCompletedEvent);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrowDomainException()
    {
        var task = CreateSut();
        task.Complete(UserId);
        task.ClearDomainEvents();

        var act = () => task.Complete(UserId);

        act.Should().Throw<DomainException>()
            .WithMessage("*already completed*");
    }

    [Fact]
    public void Complete_WhenCancelled_ShouldThrowDomainException()
    {
        var task = CreateSut();
        task.ChangeStatus(TaskStatus.Cancelled);

        var act = () => task.Complete(UserId);

        act.Should().Throw<DomainException>()
            .WithMessage("*cancelled*");
    }

    // ── ChangeStatus ──────────────────────────────────────────────────────────

    [Fact]
    public void ChangeStatus_ToInProgress_ShouldUpdateStatusAndRaiseEvent()
    {
        var task = CreateSut();
        task.ClearDomainEvents();

        task.ChangeStatus(TaskStatus.InProgress);

        task.Status.Should().Be(TaskStatus.InProgress);
        task.DomainEvents.Should().ContainSingle(e => e is TaskStatusChangedEvent);
    }

    [Fact]
    public void ChangeStatus_ToCompleted_ShouldThrowDomainException()
    {
        var task = CreateSut();

        var act = () => task.ChangeStatus(TaskStatus.Completed);

        act.Should().Throw<DomainException>()
            .WithMessage("*Complete()*");
    }

    [Fact]
    public void ChangeStatus_WhenCancelled_ShouldThrowDomainException()
    {
        var task = CreateSut();
        task.ChangeStatus(TaskStatus.Cancelled);

        var act = () => task.ChangeStatus(TaskStatus.InProgress);

        act.Should().Throw<DomainException>()
            .WithMessage("*cancelled*");
    }

    // ── Assign ────────────────────────────────────────────────────────────────

    [Fact]
    public void Assign_ShouldUpdateAssigneeAndRaiseEvent()
    {
        var task = CreateSut();
        var assigneeId = Guid.NewGuid();
        task.ClearDomainEvents();

        task.Assign(assigneeId);

        task.AssigneeId.Should().Be(assigneeId);
        task.DomainEvents.Should().ContainSingle(e => e is TaskAssignedEvent);
        var evt = task.DomainEvents.OfType<TaskAssignedEvent>().Single();
        evt.AssigneeId.Should().Be(assigneeId);
        evt.PreviousAssigneeId.Should().BeNull();
    }

    [Fact]
    public void Assign_WhenAlreadyAssigned_ShouldTrackPreviousAssignee()
    {
        var task = CreateSut();
        var firstAssigneeId = Guid.NewGuid();
        var secondAssigneeId = Guid.NewGuid();

        task.Assign(firstAssigneeId);
        task.ClearDomainEvents();
        task.Assign(secondAssigneeId);

        var evt = task.DomainEvents.OfType<TaskAssignedEvent>().Single();
        evt.PreviousAssigneeId.Should().Be(firstAssigneeId);
    }

    // ── UpdateHoursWorked ─────────────────────────────────────────────────────

    [Fact]
    public void UpdateHoursWorked_WithNegativeValue_ShouldThrowDomainException()
    {
        var task = CreateSut();

        var act = () => task.UpdateHoursWorked(-1);

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void UpdateHoursWorked_WithValidValue_ShouldUpdateProperty()
    {
        var task = CreateSut();

        task.UpdateHoursWorked(8.5m);

        task.HoursWorked.Should().Be(8.5m);
    }

    // ── IsOverdue ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsOverdue_WhenDueDatePassedAndPending_ShouldReturnTrue()
    {
        var task = CreateSut(dueDate: DateTime.UtcNow.AddDays(-1));

        task.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenDueDatePassedAndCompleted_ShouldReturnFalse()
    {
        var task = CreateSut(dueDate: DateTime.UtcNow.AddDays(-1));
        task.Complete(UserId);

        task.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenDueDateInFuture_ShouldReturnFalse()
    {
        var task = CreateSut(dueDate: DateTime.UtcNow.AddDays(10));

        task.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenNoDueDate_ShouldReturnFalse()
    {
        var task = CreateSut();

        task.IsOverdue().Should().BeFalse();
    }
}
