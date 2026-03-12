using MediatR;
using UpTask.Application.Features.Categories.DTOs;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Categories.Queries
{
    // ── Get My Tags ───────────────────────────────────────────────────────────────
    public record GetMyTagsQuery(Guid UserId) : IRequest<IEnumerable<TagDto>>;

    public class GetMyTagsHandler(ITagRepository repo) : IRequestHandler<GetMyTagsQuery, IEnumerable<TagDto>>
    {
        public async Task<IEnumerable<TagDto>> Handle(GetMyTagsQuery q, CancellationToken ct)
        {
            var tags = await repo.GetByUserAsync(q.UserId, ct);
            return tags.Select(t => new TagDto(t.Id, t.Name, t.Color));
        }
    }
}
