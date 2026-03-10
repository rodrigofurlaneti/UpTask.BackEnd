using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Common.Models;
using UpTask.Application.Features.Projects;
using UpTask.Domain.Enums;

namespace UpTask.API.Controllers
{
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
}
