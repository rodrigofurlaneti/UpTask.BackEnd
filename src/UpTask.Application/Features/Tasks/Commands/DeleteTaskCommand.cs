using MediatR;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Commands
{
    // ── Delete Task ───────────────────────────────────────────────────────────────
    public record DeleteTaskCommand(Guid TaskId, Guid RequesterId) : IRequest<Unit>;

    public class DeleteTaskHandler(ITaskRepository repo, IUnitOfWork uow)
        : IRequestHandler<DeleteTaskCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteTaskCommand cmd, CancellationToken ct)
        {
            var task = await repo.GetByIdAsync(cmd.TaskId, ct)
                ?? throw new NotFoundException("Task", cmd.TaskId);

            if (task.CreatedBy != cmd.RequesterId)
                throw new UnauthorizedException("Only the task creator can delete it.");

            repo.Remove(task);
            await uow.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }

}
