using FluentAssertions;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;
using Xunit;

namespace UpTask.Domain.Tests.Entities;

public sealed class ProjectTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static Project CreateSut(
        string name = "My Project",
        Priority priority = Priority.Medium,
        DateOnly? startDate = null,
        DateOnly? endDate = null) =>
        Project.Create(
            ownerId: OwnerId,
            name: new ProjectName(name),
            description: null,
            priority: priority,
            startDate: startDate,
            plannedEndDate: endDate);

    // ── Creation ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetStatusToDraft()
    {
        var project = CreateSut();

        project.Status.Should().Be(ProjectStatus.Draft);
    }

    [Fact]
    public void Create_ShouldAddOwnerAsAdminMember()
    {
        var project = CreateSut();

        project.Members.Should().ContainSingle();
        project.Members.Single().UserId.Should().Be(OwnerId);
        project.Members.Single().Role.Should().Be(MemberRole.Admin);
    }

    [Fact]
    public void Create_ShouldRaiseProjectCreatedEvent()
    {
        var project = CreateSut();

        project.DomainEvents.Should().ContainSingle(e => e is ProjectCreatedEvent);
        var evt = project.DomainEvents.OfType<ProjectCreatedEvent>().Single();
        evt.OwnerId.Should().Be(OwnerId);
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ShouldThrowDomainException()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(-1);

        var act = () => CreateSut(startDate: start, endDate: end);

        act.Should().Throw<DomainException>()
            .WithMessage("*End date*start date*");
    }

    // ── AddMember ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddMember_WithNewUser_ShouldAddToMembersList()
    {
        var project = CreateSut();
        var newUserId = Guid.NewGuid();

        project.AddMember(newUserId, MemberRole.Collaborator, OwnerId);

        project.Members.Should().HaveCount(2);
        project.IsMember(newUserId).Should().BeTrue();
    }

    [Fact]
    public void AddMember_WithDuplicateUser_ShouldThrowDomainException()
    {
        var project = CreateSut();
        var newUserId = Guid.NewGuid();
        project.AddMember(newUserId, MemberRole.Collaborator, OwnerId);

        var act = () => project.AddMember(newUserId, MemberRole.Editor, OwnerId);

        act.Should().Throw<DomainException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public void AddMember_ShouldRaiseProjectMemberAddedEvent()
    {
        var project = CreateSut();
        project.ClearDomainEvents();
        var newUserId = Guid.NewGuid();

        project.AddMember(newUserId, MemberRole.Editor, OwnerId);

        project.DomainEvents.Should().ContainSingle(e => e is ProjectMemberAddedEvent);
    }

    // ── RemoveMember ──────────────────────────────────────────────────────────

    [Fact]
    public void RemoveMember_ShouldRemoveFromMembersList()
    {
        var project = CreateSut();
        var newUserId = Guid.NewGuid();
        project.AddMember(newUserId, MemberRole.Collaborator, OwnerId);

        project.RemoveMember(newUserId);

        project.IsMember(newUserId).Should().BeFalse();
    }

    [Fact]
    public void RemoveMember_WhenRemovingOwner_ShouldThrowDomainException()
    {
        var project = CreateSut();

        var act = () => project.RemoveMember(OwnerId);

        act.Should().Throw<DomainException>()
            .WithMessage("*owner*");
    }

    // ── UpdateProgress ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateProgress_At100Percent_WhenActive_ShouldAutoCompleteProject()
    {
        var project = CreateSut();
        project.ChangeStatus(ProjectStatus.Active);
        project.ClearDomainEvents();

        project.UpdateProgress(totalTasks: 4, completedTasks: 4);

        project.Status.Should().Be(ProjectStatus.Completed);
        project.ActualEndDate.Should().NotBeNull();
        project.DomainEvents.Should().ContainSingle(e => e is ProjectCompletedEvent);
    }

    [Fact]
    public void UpdateProgress_WithNoTasks_ShouldReturnZeroPercent()
    {
        var project = CreateSut();

        project.UpdateProgress(0, 0);

        project.Progress.Should().Be(0);
    }

    // ── ChangeStatus ──────────────────────────────────────────────────────────

    [Fact]
    public void ChangeStatus_WhenCancelled_ShouldThrowDomainException()
    {
        var project = CreateSut();
        project.ChangeStatus(ProjectStatus.Cancelled);

        var act = () => project.ChangeStatus(ProjectStatus.Active);

        act.Should().Throw<DomainException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_ShouldThrowDomainException()
    {
        var project = CreateSut();

        var act = () => project.ChangeStatus(ProjectStatus.Draft);

        act.Should().Throw<DomainException>()
            .WithMessage("*already*");
    }
}
