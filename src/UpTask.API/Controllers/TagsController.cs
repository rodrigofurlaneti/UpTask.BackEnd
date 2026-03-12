using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Categories.Commands; // Onde mora o CreateTagCommand
using UpTask.Application.Features.Categories.Queries;  // Onde mora o GetMyTagsQuery
using UpTask.Application.Features.Categories.DTOs;

namespace UpTask.API.Controllers
{
    [Route("api/v1/tags")]
    public sealed class TagsController(ISender sender, ICurrentUserService currentUser)
        : ApiController(sender)
    {
        [HttpGet]
        public async Task<IActionResult> GetMine(CancellationToken ct)
        {
            var result = await Sender.Send(new GetMyTagsQuery(currentUser.UserId), ct);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTagCommand cmd, CancellationToken ct)
        {
            var result = await Sender.Send(cmd with { UserId = currentUser.UserId }, ct);
            return Ok(result);
        }
    }
}