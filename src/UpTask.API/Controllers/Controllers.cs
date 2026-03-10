using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Common.Models;
using UpTask.Application.Features.Auth.Commands;
using UpTask.Application.Features.Categories;
using UpTask.Application.Features.Projects;
using UpTask.Application.Features.Tasks;
using UpTask.Application.Features.TimeTracking;
using UpTask.Domain.Enums;
using TaskStatus = UpTask.Domain.Enums.TaskStatus;

namespace UpTask.API.Controllers;

// ── AUTH ──────────────────────────────────────────────────────────────────────
[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController(ISender mediator, ICurrentUserService currentUser)
    : ApiController(mediator, currentUser)
{
    /// <summary>Register a new account</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 422)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RegisterCommand(dto.Name, dto.Email, dto.Password, dto.ConfirmPassword), ct);
        return Ok(ApiResponse<AuthTokenDto>.Ok(result, "Account created successfully."));
    }

    /// <summary>Login and receive JWT token</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);
        return Ok(ApiResponse<AuthTokenDto>.Ok(result));
    }

    /// <summary>Change current user password</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand cmd, CancellationToken ct)
    {
        await _mediator.Send(cmd with { UserId = CurrentUserId }, ct);
        return Ok(ApiResponse.Ok("Password changed successfully."));
    }
}

// ── PROJECTS ──────────────────────────────────────────────────────────────────
[Route("api/v1/projects")]
public sealed class ProjectsController(ISender mediator, ICurrentUserService currentUser)
    : ApiController(mediator, currentUser)
{
    /// <summary>Get all projects I'm a member of</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProjectDto>>), 200)]
    public async Task<IActionResult> GetMyProjects(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyProjectsQuery(CurrentUserId), ct);
        return Ok(ApiResponse<IEnumerable<ProjectDto>>.Ok(result));
    }

    /// <summary>Get a specific project</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id, CurrentUserId), ct);
        return Ok(ApiResponse<ProjectDto>.Ok(result));
    }

    /// <summary>Create a new project</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateProjectCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd with { OwnerId = CurrentUserId }, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ProjectDto>.Ok(result, "Project created."));
    }

    /// <summary>Update a project</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd with { ProjectId = id, RequesterId = CurrentUserId }, ct);
        return Ok(ApiResponse<ProjectDto>.Ok(result));
    }

    /// <summary>Change project status</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ProjectStatus newStatus, CancellationToken ct)
    {
        await _mediator.Send(new ChangeProjectStatusCommand(id, CurrentUserId, newStatus), ct);
        return Ok(ApiResponse.Ok("Status updated."));
    }

    /// <summary>Delete a project</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProjectCommand(id, CurrentUserId), ct);
        return Ok(ApiResponse.Ok("Project deleted."));
    }

    /// <summary>Add a member to the project</summary>
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest req, CancellationToken ct)
    {
        await _mediator.Send(new AddProjectMemberCommand(id, CurrentUserId, req.UserId, req.Role), ct);
        return Ok(ApiResponse.Ok("Member added."));
    }
}

public record AddMemberRequest(Guid UserId, MemberRole Role);

// ── TASKS ─────────────────────────────────────────────────────────────────────
[Route("api/v1/tasks")]
public sealed class TasksController(ISender mediator, ICurrentUserService currentUser)
    : ApiController(mediator, currentUser)
{
    /// <summary>Get my assigned tasks</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TaskDto>>), 200)]
    public async Task<IActionResult> GetMyTasks(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyTasksQuery(CurrentUserId), ct);
        return Ok(ApiResponse<IEnumerable<TaskDto>>.Ok(result));
    }

    /// <summary>Get tasks of a project</summary>
    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetProjectTasks(Guid projectId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectTasksQuery(projectId, CurrentUserId), ct);
        return Ok(ApiResponse<IEnumerable<TaskDto>>.Ok(result));
    }

    /// <summary>Get task details</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTaskByIdQuery(id, CurrentUserId), ct);
        return Ok(ApiResponse<TaskDetailDto>.Ok(result));
    }

    /// <summary>Create a new task</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd with { CreatedBy = CurrentUserId }, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<TaskDto>.Ok(result, "Task created."));
    }

    /// <summary>Update task</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd with { TaskId = id, RequesterId = CurrentUserId }, ct);
        return Ok(ApiResponse<TaskDto>.Ok(result));
    }

    /// <summary>Change task status</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] TaskStatus newStatus, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeTaskStatusCommand(id, CurrentUserId, newStatus), ct);
        return Ok(ApiResponse<TaskDto>.Ok(result));
    }

    /// <summary>Complete a task</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteTaskCommand(id, CurrentUserId), ct);
        return Ok(ApiResponse<TaskDto>.Ok(result, "Task completed!"));
    }

    /// <summary>Assign task to user</summary>
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTaskRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignTaskCommand(id, CurrentUserId, req.AssigneeId), ct);
        return Ok(ApiResponse<TaskDto>.Ok(result));
    }

    /// <summary>Add comment to task</summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddCommentCommand(id, CurrentUserId, req.Content), ct);
        return Ok(ApiResponse<CommentDto>.Ok(result));
    }

    /// <summary>Delete task</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteTaskCommand(id, CurrentUserId), ct);
        return Ok(ApiResponse.Ok("Task deleted."));
    }
}

public record AssignTaskRequest(Guid AssigneeId);
public record AddCommentRequest(string Content);

// ── TIME TRACKING ─────────────────────────────────────────────────────────────
[Route("api/v1/time")]
public sealed class TimeTrackingController(ISender mediator, ICurrentUserService currentUser)
    : ApiController(mediator, currentUser)
{
    /// <summary>Log time on a task</summary>
    [HttpPost]
    public async Task<IActionResult> LogTime([FromBody] LogTimeCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd with { UserId = CurrentUserId }, ct);
        return Ok(ApiResponse<TimeEntryDto>.Ok(result, "Time logged."));
    }

    /// <summary>Get time entries for a task</summary>
    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> GetTaskEntries(Guid taskId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTaskTimeEntriesQuery(taskId, CurrentUserId), ct);
        return Ok(ApiResponse<IEnumerable<TimeEntryDto>>.Ok(result));
    }

    /// <summary>Delete a time entry</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteTimeEntryCommand(id, CurrentUserId), ct);
        return Ok(ApiResponse.Ok("Time entry deleted."));
    }
}

// ── CATEGORIES ────────────────────────────────────────────────────────────────
[Route("api/v1/categories")]
public sealed class CategoriesController(ISender mediator, ICurrentUserService currentUser)
    : ApiController(mediator, currentUser)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(CurrentUserId), ct);
        return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd with { UserId = CurrentUserId }, ct);
        return Ok(ApiResponse<CategoryDto>.Ok(result));
    }
}

// ── TAGS ──────────────────────────────────────────────────────────────────────
[Route("api/v1/tags")]
public sealed class TagsController(ISender mediator, ICurrentUserService currentUser)
    : ApiController(mediator, currentUser)
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyTagsQuery(CurrentUserId), ct);
        return Ok(ApiResponse<IEnumerable<TagDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTagCommand cmd, CancellationToken ct)
    {
        var result = await _mediator.Send(cmd with { UserId = CurrentUserId }, ct);
        return Ok(ApiResponse<TagDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteTagCommand(id, CurrentUserId), ct);
        return Ok(ApiResponse.Ok("Tag deleted."));
    }
}