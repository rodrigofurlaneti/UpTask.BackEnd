using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Common.Models;
using UpTask.Application.Features.Categories.DTOs;
using UpTask.Application.Features.Categories.Queries;
namespace UpTask.API.Controllers
{
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
        public async Task<IActionResult> Create([FromBody] TagDto cmd, CancellationToken ct)
        {
            var result = await _mediator.Send(cmd with { UserId = CurrentUserId }, ct);
            return Ok(ApiResponse<TagDto>.Ok(result));
        }
    }
}
