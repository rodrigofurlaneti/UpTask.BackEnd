using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.TimeTracking.Commands
{
    // ── Delete Time Entry ─────────────────────────────────────────────────────────
    public record DeleteTimeEntryCommand(Guid EntryId, Guid RequesterId) : IRequest<Unit>;

    public class DeleteTimeEntryHandler(ITimeEntryRepository timeRepo, ITaskRepository taskRepo, IUnitOfWork uow)
        : IRequestHandler<DeleteTimeEntryCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteTimeEntryCommand cmd, CancellationToken ct)
        {
            var entry = await timeRepo.GetByIdAsync(cmd.EntryId, ct)
                ?? throw new NotFoundException("TimeEntry", cmd.EntryId);

            if (entry.UserId != cmd.RequesterId)
                throw new UnauthorizedException("You can only delete your own time entries.");

            timeRepo.Remove(entry);
            await uow.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
