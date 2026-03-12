using MediatR;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Categories.Commands
{
    // ── Delete Tag ────────────────────────────────────────────────────────────────
    public record DeleteTagCommand(Guid TagId, Guid UserId) : IRequest<Unit>;

    public class DeleteTagHandler(ITagRepository repo, IUnitOfWork uow) : IRequestHandler<DeleteTagCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteTagCommand cmd, CancellationToken ct)
        {
            var tag = await repo.GetByIdAsync(cmd.TagId, ct)
                ?? throw new NotFoundException("Tag", cmd.TagId);

            if (tag.UserId != cmd.UserId) throw new UnauthorizedException("Cannot delete another user's tag.");
            repo.Remove(tag);
            await uow.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
