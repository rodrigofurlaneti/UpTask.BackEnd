using FluentAssertions;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Events;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;
using Xunit;

namespace UpTask.UnitTests.Domain;

// ── Email Value Object ─────────────────────────────────────────────────────────
public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user.name+tag@domain.co")]
    public void Create_WithValidEmail_ShouldSucceed(string email)
    {
        var act = () => new Email(email);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@nodomain.com")]
    [InlineData("noatsign")]
    public void Create_WithInvalidEmail_ShouldThrowBusinessRuleException(string email)
    {
        var act = () => new Email(email);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldNormalizeToLowercase()
    {
        var email = new Email("USER@EXAMPLE.COM");
        email.Value.Should().Be("user@example.com");
    }
}

// ── User Entity ────────────────────────────────────────────────────────────────
public class UserTests
{
    private static User CreateValidUser() =>
        User.Create("John Doe", new Email("john@example.com"), "hashed_password");

    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        var user = CreateValidUser();
        user.Name.Should().Be("John Doe");
        user.Email.Value.Should().Be("john@example.com");
        user.Status.Should().Be(UserStatus.Active);
        user.Profile.Should().Be(UserProfile.Member);
    }

    [Fact]
    public void Create_ShouldRaiseUserCreatedEvent()
    {
        var user = CreateValidUser();
        user.DomainEvents.Should().ContainSingle(e => e is UserCreatedEvent);
    }

    [Fact]
    public void Create_ShouldInitializeSettings()
    {
        var user = CreateValidUser();
        user.Settings.Should().NotBeNull();
        user.Settings!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => User.Create("", new Email("john@example.com"), "hash");
        act.Should().Throw<BusinessRuleException>().WithMessage("*Name is required*");
    }

    [Fact]
    public void Suspend_ActiveUser_ShouldSetStatusSuspended()
    {
        var user = CreateValidUser();
        user.Suspend();
        user.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public void Suspend_AlreadySuspended_ShouldThrow()
    {
        var user = CreateValidUser();
        user.Suspend();
        var act = () => user.Suspend();
        act.Should().Throw<BusinessRuleException>().WithMessage("*already suspended*");
    }

    [Fact]
    public void ChangePassword_ShouldUpdateHashAndClearToken()
    {
        var user = CreateValidUser();
        user.SetPasswordResetToken("token123", DateTime.UtcNow.AddHours(1));
        user.ChangePassword("new_hash");
        user.PasswordHash.Should().Be("new_hash");
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiresAt.Should().BeNull();
    }
}

// ── Project Entity ─────────────────────────────────────────────────────────────
public class ProjectTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static Project CreateValidProject() =>
        Project.Create(OwnerId, "My Project", "Description",
            Priority.Medium, DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)));

    [Fact]
    public void Create_WithValidData_ShouldCreateProject()
    {
        var project = CreateValidProject();
        project.Name.Should().Be("My Project");
        project.OwnerId.Should().Be(OwnerId);
        project.Status.Should().Be(ProjectStatus.Draft);
        project.Progress.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldAutomaticallyAddOwnerAsMember()
    {
        var project = CreateValidProject();
        project.Members.Should().HaveCount(1);
        project.Members.First().UserId.Should().Be(OwnerId);
        project.Members.First().Role.Should().Be(MemberRole.Admin);
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ShouldThrow()
    {
        var act = () => Project.Create(OwnerId, "Project", null, Priority.Low,
            DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            DateOnly.FromDateTime(DateTime.Today));
        act.Should().Throw<BusinessRuleException>().WithMessage("*End date*");
    }

    [Fact]
    public void AddMember_NewUser_ShouldAddSuccessfully()
    {
        var project = CreateValidProject();
        var newUserId = Guid.NewGuid();
        project.AddMember(newUserId, MemberRole.Collaborator, OwnerId);
        project.Members.Should().HaveCount(2);
        project.IsMember(newUserId).Should().BeTrue();
    }

    [Fact]
    public void AddMember_DuplicateUser_ShouldThrow()
    {
        var project = CreateValidProject();
        var act = () => project.AddMember(OwnerId, MemberRole.Viewer, OwnerId);
        act.Should().Throw<BusinessRuleException>().WithMessage("*already a member*");
    }

    [Fact]
    public void RemoveMember_Owner_ShouldThrow()
    {
        var project = CreateValidProject();
        var act = () => project.RemoveMember(OwnerId);
        act.Should().Throw<BusinessRuleException>().WithMessage("*project owner*");
    }

    [Fact]
    public void UpdateProgress_WhenAllComplete_ShouldMarkProjectCompleted()
    {
        var project = CreateValidProject();
        project.ChangeStatus(ProjectStatus.Active);
        project.UpdateProgress(5, 5);
        project.Progress.Should().Be(100);
        project.Status.Should().Be(ProjectStatus.Completed);
        project.ActualEndDate.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProgress_Partial_ShouldCalculateCorrectly()
    {
        var project = CreateValidProject();
        project.UpdateProgress(10, 7);
        project.Progress.Should().Be(70);
    }
}

// ── TaskItem Entity ────────────────────────────────────────────────────────────
public class TaskItemTests
{
    private static readonly Guid CreatedBy = Guid.NewGuid();

    private static TaskItem CreateValidTask() =>
        TaskItem.Create(CreatedBy, "Fix bug #123", "Description",
            Priority.High, DateTime.UtcNow.AddDays(7));

    [Fact]
    public void Create_WithValidData_ShouldCreateTask()
    {
        var task = CreateValidTask();
        task.Title.Should().Be("Fix bug #123");
        task.Status.Should().Be(Domain.Enums.TaskStatus.Pending);
        task.CreatedBy.Should().Be(CreatedBy);
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        var act = () => TaskItem.Create(CreatedBy, "", null, Priority.Low, null);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Complete_PendingTask_ShouldSetCompletedStatus()
    {
        var task = CreateValidTask();
        task.Complete(CreatedBy);
        task.Status.Should().Be(Domain.Enums.TaskStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_AlreadyCompleted_ShouldThrow()
    {
        var task = CreateValidTask();
        task.Complete(CreatedBy);
        var act = () => task.Complete(CreatedBy);
        act.Should().Throw<BusinessRuleException>().WithMessage("*already completed*");
    }

    [Fact]
    public void Complete_CancelledTask_ShouldThrow()
    {
        var task = CreateValidTask();
        task.ChangeStatus(Domain.Enums.TaskStatus.Cancelled);
        var act = () => task.Complete(CreatedBy);
        act.Should().Throw<BusinessRuleException>().WithMessage("*cancelled*");
    }

    [Fact]
    public void IsOverdue_WhenPastDueDate_ShouldReturnTrue()
    {
        var task = TaskItem.Create(CreatedBy, "Overdue", null, Priority.Low,
            DateTime.UtcNow.AddDays(-1));
        task.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenCompleted_ShouldReturnFalse()
    {
        var task = TaskItem.Create(CreatedBy, "Done", null, Priority.Low,
            DateTime.UtcNow.AddDays(-1));
        task.Complete(CreatedBy);
        task.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void SetRecurrence_ShouldSetFields()
    {
        var task = CreateValidTask();
        task.SetRecurrence(RecurrenceType.Weekly, DateOnly.FromDateTime(DateTime.Today.AddDays(7)));
        task.IsRecurring.Should().BeTrue();
        task.RecurrenceType.Should().Be(RecurrenceType.Weekly);
    }

    [Fact]
    public void UpdateHoursWorked_Negative_ShouldThrow()
    {
        var task = CreateValidTask();
        var act = () => task.UpdateHoursWorked(-1);
        act.Should().Throw<BusinessRuleException>();
    }
}

// ── TimeEntry Entity ───────────────────────────────────────────────────────────
public class TimeEntryTests
{
    [Fact]
    public void Create_WithValidTimes_ShouldCalculateDuration()
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2024, 1, 1, 9, 0, 0),
            new DateTime(2024, 1, 1, 11, 30, 0), "Work done");

        entry.DurationMinutes.Should().Be(150);
    }

    [Fact]
    public void Create_EndBeforeStart_ShouldThrow()
    {
        var act = () => TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, DateTime.UtcNow.AddHours(-1), null);
        act.Should().Throw<BusinessRuleException>().WithMessage("*End time*");
    }
}

// ── TaskDependency Entity ──────────────────────────────────────────────────────
public class TaskDependencyTests
{
    [Fact]
    public void Create_SelfDependency_ShouldThrow()
    {
        var id = Guid.NewGuid();
        var act = () => TaskDependency.Create(id, id, DependencyType.Blocks);
        act.Should().Throw<BusinessRuleException>().WithMessage("*cannot depend on itself*");
    }

    [Fact]
    public void Create_DifferentTasks_ShouldSucceed()
    {
        var dep = TaskDependency.Create(Guid.NewGuid(), Guid.NewGuid(), DependencyType.Blocks);
        dep.Should().NotBeNull();
    }
}

// ── Comment Entity ─────────────────────────────────────────────────────────────
public class CommentTests
{
    [Fact]
    public void Edit_DeletedComment_ShouldThrow()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "original");
        comment.SoftDelete();
        var act = () => comment.Edit("new content");
        act.Should().Throw<BusinessRuleException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Edit_ShouldMarkAsEdited()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "original");
        comment.Edit("edited content");
        comment.IsEdited.Should().BeTrue();
        comment.EditedAt.Should().NotBeNull();
        comment.Content.Should().Be("edited content");
    }
}
