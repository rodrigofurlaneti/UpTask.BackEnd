using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.TimeTracking.Queries;
using UpTask.Application.Features.TimeTracking.Commands;
using UpTask.Application.Features.TimeTracking.DTOs;

namespace UpTask.API.Controllers;

[Route("api/v1/time")]
public sealed class TimeTrackingController(ISender sender, ICurrentUserService currentUser)
    : ApiController(sender)
{
    /// <summary>Log time on a task</summary>
    [HttpPost]
    public async Task<IActionResult> LogTime([FromBody] LogTimeCommand cmd, CancellationToken ct)
    {
        // Garante que o UserId venha do token (ICurrentUserService)
        var result = await Sender.Send(cmd with { UserId = currentUser.UserId }, ct);
        return Ok(result);
    }

    /// <summary>Get time entries for a task</summary>
    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> GetTaskEntries(Guid taskId, CancellationToken ct)
    {
        // Resolve o erro CS1729: O record precisa receber taskId e UserId
        var result = await Sender.Send(new GetTaskTimeEntriesQuery(taskId, currentUser.UserId), ct);
        return Ok(result);
    }

    /// <summary>Delete a time entry</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        // Resolve o erro CS1503: O comando deve retornar Result e não Unit
        var result = await Sender.Send(new DeleteTimeEntryCommand(id, currentUser.UserId), ct);
        return NoContent(result);
    }
}