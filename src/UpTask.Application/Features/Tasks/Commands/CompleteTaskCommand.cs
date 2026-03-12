using MediatR;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper; // Adicionado para o TaskMapper
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Commands
{
    public record CompleteTaskCommand(Guid TaskId, Guid UserId) : IRequest<TaskDto>;

    public class CompleteTaskHandler(ITaskRepository repo, IUnitOfWork uow)
        : IRequestHandler<CompleteTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(CompleteTaskCommand cmd, CancellationToken ct)
        {
            var task = await repo.GetByIdAsync(cmd.TaskId, ct)
                ?? throw new NotFoundException("Task", cmd.TaskId);

            // Executa a lógica de conclusão no domínio
            task.Complete(cmd.UserId);

            await uow.SaveChangesAsync(ct);

            // Agora o TaskMapper é reconhecido
            return TaskMapper.ToDto(task);
        }
    }
}