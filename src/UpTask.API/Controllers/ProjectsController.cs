using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Features.Projects.Commands;
using UpTask.Application.Features.Projects.DTOs;
using UpTask.Application.Features.Projects.Queries;
using UpTask.Domain.Enums;

namespace UpTask.API.Controllers;

/// <summary>Manages projects, their members, and status transitions.</summary>
[Route("api/projects")]
public sealed class ProjectsController(ISender sender) : ApiController(sender)
{
    /// <summary>Lists all projects where the current user is a member.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await Sender.Send(new GetMyProjectsQuery(), ct);
        return Ok(new { data = result });
    }

    /// <summary>Creates a new project. The current user becomes the owner and first admin.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateProjectCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        if (!result.IsSuccess) return Ok(result);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Updates project metadata. Requires Admin role on the project.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateProjectCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { ProjectId = id }, ct);
        return Ok(result);
    }

    /// <summary>Deletes a project. Only the owner can delete it.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new DeleteProjectCommand(id), ct);
        return NoContent(result);
    }

    /// <summary>Changes the project status (Draft → Active → Paused → Completed/Cancelled).</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ProjectStatus newStatus, CancellationToken ct)
    {
        var result = await Sender.Send(new ChangeProjectStatusCommand(id, newStatus), ct);
        return NoContent(result);
    }

    /// <summary>Adds a member to the project. Requires Admin role.</summary>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(Guid id, AddProjectMemberCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { ProjectId = id }, ct);
        return NoContent(result);
    }

    /// <summary>Removes a member from the project. Cannot remove the owner.</summary>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        var result = await Sender.Send(new RemoveProjectMemberCommand(id, userId), ct);
        return NoContent(result);
    }
}
