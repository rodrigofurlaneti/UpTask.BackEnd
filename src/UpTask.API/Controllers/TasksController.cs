using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Tasks.Commands;
using UpTask.Application.Features.Tasks.Queries;
using UpTask.Application.Features.Tasks.DTOs;

namespace UpTask.API.Controllers;

/// <summary>
/// Manages task lifecycle: create, read, update, status changes, assignment, and deletion.
/// All endpoints require authentication (inherited from ApiController).
/// </summary>
[Route("api/tasks")]
public sealed class TasksController(ISender sender, ICurrentUserService currentUser) : ApiController(sender)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetTaskByIdQuery(id, currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTasks(CancellationToken ct)
    {
        var result = await Sender.Send(new GetMyTasksQuery(currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectTasks(Guid projectId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetProjectTasksQuery(projectId, currentUser.UserId), ct);
        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateTaskCommand command, CancellationToken ct)
    {
        // Forçamos o tipo Result<TaskDto> para o compilador não se confundir
        UpTask.Domain.Common.Result<TaskDto> result = await Sender.Send(command with { CreatedBy = currentUser.UserId }, ct);

        // Agora o compilador sabe que 'result' TEM IsSuccess
        if (!result.IsSuccess)
        {
            return Ok(result); // O ApiController trata a falha
        }

        // Agora ele sabe que 'result' TEM Value e Value TEM Id
        var taskId = result.Value.Id;

        return Created<TaskDto>(nameof(GetById), new { id = taskId }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, UpdateTaskCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { TaskId = id, UserId = currentUser.UserId }, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeTaskStatusCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { TaskId = id, UserId = currentUser.UserId }, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CompleteTaskCommand(id, currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignTaskCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { TaskId = id, AdminId = currentUser.UserId }, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteTaskCommand(id, currentUser.UserId), ct);
        return NoContent(result);
    }
}