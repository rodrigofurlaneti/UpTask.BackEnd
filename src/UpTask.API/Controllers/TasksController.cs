using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Features.Tasks.Commands;
using UpTask.Application.Features.Tasks.Queries;
using UpTask.Application.Features.Tasks.DTOs;

namespace UpTask.API.Controllers;

/// <summary>
/// Manages task lifecycle: create, read, update, status changes, assignment, and deletion.
/// All endpoints require authentication (inherited from ApiController).
/// </summary>
[Route("api/tasks")]
public sealed class TasksController(ISender sender) : ApiController(sender)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Gets a single task with its sub-tasks, comments, and checklists.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetTaskByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Lists all tasks assigned to the current user.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTasks(CancellationToken ct)
    {
        var result = await Sender.Send(new GetMyTasksQuery(), ct);
        return Ok(result);
    }

    /// <summary>Lists all tasks in a project (requires project membership).</summary>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectTasks(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetProjectTasksQuery(projectId), ct);
        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Creates a new task. ProjectId is optional (standalone task).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateTaskCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return Created(nameof(GetById), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty }, result);
    }

    /// <summary>Updates task fields (title, description, priority, dates, etc.).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateTaskCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { TaskId = id }, ct);
        return Ok(result);
    }

    /// <summary>Transitions the task to a new status. Use PATCH for partial updates.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeTaskStatusCommand command,
        CancellationToken ct)
    {
        var result = await Sender.Send(command with { TaskId = id }, ct);
        return Ok(result);
    }

    /// <summary>Marks the task as completed by the current user.</summary>
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CompleteTaskCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Assigns the task to a specific user (must be a project member).</summary>
    [HttpPatch("{id:guid}/assign")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(Guid id, AssignTaskCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { TaskId = id }, ct);
        return Ok(result);
    }

    /// <summary>Permanently deletes a task (only the creator can do this).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteTaskCommand(id), ct);
        return NoContent(result);
    }
}
