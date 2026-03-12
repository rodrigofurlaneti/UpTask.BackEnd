using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Categories.Commands;
using UpTask.Application.Features.Categories.Queries;
using UpTask.Application.Features.Categories.DTOs;

namespace UpTask.API.Controllers
{
    [Route("api/v1/categories")]
    public sealed class CategoriesController(ISender sender, ICurrentUserService currentUser)
        : ApiController(sender)
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await Sender.Send(new GetCategoriesQuery(currentUser.UserId), ct);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand cmd, CancellationToken ct)
        {
            var result = await Sender.Send(cmd with { UserId = currentUser.UserId }, ct);
            return Ok(result);
        }
    }
}