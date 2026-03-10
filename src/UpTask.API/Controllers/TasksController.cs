using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Common.Models;
using UpTask.Application.Features.Tasks;

namespace UpTask.API.Controllers
{
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
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] UpTask.Domain.Enums.TaskStatus newStatus, CancellationToken ct)
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
}
