using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using UpTask.Application.Features.Auth.Commands;
using UpTask.Application.Features.Projects;
using UpTask.Application.Features.Tasks;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects;
using Xunit;

namespace UpTask.UnitTests.Application;

// ── Register Handler ───────────────────────────────────────────────────────────
public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPasswordService _passwordService = Substitute.For<IPasswordService>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private RegisterCommandHandler CreateHandler() =>
        new(_userRepo, _uow, _passwordService, _jwtService);

    [Fact]
    public async Task Handle_NewUser_ShouldRegisterAndReturnToken()
    {
        _userRepo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordService.Hash(Arg.Any<string>()).Returns("hashed_pw");
        _jwtService.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("jwt_token");

        var cmd = new RegisterCommand("Alice", "alice@example.com", "Pass@1234", "Pass@1234");
        var result = await CreateHandler().Handle(cmd, default);

        result.AccessToken.Should().Be("jwt_token");
        result.Email.Should().Be("alice@example.com");
        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingEmail_ShouldThrowBusinessRuleException()
    {
        _userRepo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var cmd = new RegisterCommand("Bob", "bob@example.com", "Pass@1234", "Pass@1234");

        var act = async () => await CreateHandler().Handle(cmd, default);
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already registered*");
    }
}

// ── Login Handler ──────────────────────────────────────────────────────────────
public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPasswordService _passwordService = Substitute.For<IPasswordService>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private LoginCommandHandler CreateHandler() =>
        new(_userRepo, _uow, _passwordService, _jwtService);

    private static User CreateActiveUser()
        => User.Create("Test", new Email("test@example.com"), "hash");

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnToken()
    {
        var user = CreateActiveUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("token_abc");

        var result = await CreateHandler().Handle(new LoginCommand("test@example.com", "pass"), default);

        result.AccessToken.Should().Be("token_abc");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowUnauthorized()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsNull();
        var act = async () => await CreateHandler().Handle(new LoginCommand("x@x.com", "pw"), default);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldThrowUnauthorized()
    {
        var user = CreateActiveUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = async () => await CreateHandler().Handle(new LoginCommand("test@example.com", "wrong"), default);
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*Invalid credentials*");
    }
}

// ── Create Project Handler ─────────────────────────────────────────────────────
public class CreateProjectHandlerTests
{
    private readonly IProjectRepository _repo = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnProject()
    {
        var cmd = new CreateProjectCommand("New Project", null, Priority.High, null, null, null, Guid.NewGuid());
        var result = await new CreateProjectHandler(_repo, _uow).Handle(cmd, default);

        result.Name.Should().Be("New Project");
        result.Priority.Should().Be(Priority.High);
        await _repo.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

// ── Update Project Handler ─────────────────────────────────────────────────────
public class UpdateProjectHandlerTests
{
    private readonly IProjectRepository _repo = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_NonAdmin_ShouldThrowUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid(); // different user
        var project = Project.Create(ownerId, "P", null, Priority.Low, null, null);

        _repo.GetWithMembersAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var cmd = new UpdateProjectCommand(project.Id, requesterId, "New Name", null,
            Priority.Low, null, null, "#fff", null);

        var act = async () => await new UpdateProjectHandler(_repo, _uow).Handle(cmd, default);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldThrowNotFound()
    {
        _repo.GetWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ReturnsNull();
        var cmd = new UpdateProjectCommand(Guid.NewGuid(), Guid.NewGuid(), "Name", null,
            Priority.Low, null, null, "#fff", null);

        var act = async () => await new UpdateProjectHandler(_repo, _uow).Handle(cmd, default);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

// ── Create Task Handler ────────────────────────────────────────────────────────
public class CreateTaskHandlerTests
{
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_WithoutProject_ShouldCreatePersonalTask()
    {
        var cmd = new CreateTaskCommand(Guid.NewGuid(), "My Task", null, Priority.Low,
            null, null, null, null, null, null);

        var result = await new CreateTaskHandler(_taskRepo, _projectRepo, _uow).Handle(cmd, default);

        result.Title.Should().Be("My Task");
        result.ProjectId.Should().BeNull();
        await _taskRepo.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProject_NonMember_ShouldThrow()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create(ownerId, "P", null, Priority.Low, null, null);
        _projectRepo.GetWithMembersAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var nonMember = Guid.NewGuid();
        var cmd = new CreateTaskCommand(nonMember, "Task", null, Priority.Low,
            null, project.Id, null, null, null, null);

        var act = async () => await new CreateTaskHandler(_taskRepo, _projectRepo, _uow).Handle(cmd, default);
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*not a member*");
    }
}

// ── Complete Task Handler ──────────────────────────────────────────────────────
public class CompleteTaskHandlerTests
{
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ValidTask_ShouldComplete()
    {
        var userId = Guid.NewGuid();
        var task = TaskItem.Create(userId, "Task", null, Priority.Medium, null);
        _taskRepo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _projectRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await new CompleteTaskHandler(_taskRepo, _projectRepo, _uow)
            .Handle(new CompleteTaskCommand(task.Id, userId), default);

        result.Status.Should().Be(Domain.Enums.TaskStatus.Completed);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ShouldThrow()
    {
        _taskRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ReturnsNull();
        var act = async () => await new CompleteTaskHandler(_taskRepo, _projectRepo, _uow)
            .Handle(new CompleteTaskCommand(Guid.NewGuid(), Guid.NewGuid()), default);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
