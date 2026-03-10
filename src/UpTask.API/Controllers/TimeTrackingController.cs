using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Common.Models;
using UpTask.Application.Features.TimeTracking;

namespace UpTask.API.Controllers
{
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
}
