using MediatR;
using UpTask.Domain.Common;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Commands
{
    public record DeleteTaskCommand(Guid TaskId, Guid RequesterId) : IRequest<Result>;
    public class DeleteTaskHandler(ITaskRepository repo, IUnitOfWork uow)
        : IRequestHandler<DeleteTaskCommand, Result>
    {
        public async Task<Result> Handle(DeleteTaskCommand cmd, CancellationToken ct)
        {
            var task = await repo.GetByIdAsync(cmd.TaskId, ct);
            if (task is null)
                return Result.Failure(Error.NotFound("Tasks.NotFound", "Task not found."));
            if (task.CreatedBy != cmd.RequesterId)
                return Result.Failure(new Error("Tasks.Unauthorized", "Only the task creator can delete it."));
            repo.Remove(task);
            await uow.SaveChangesAsync(ct);
            return Result.Success(); 
        }
    }
}