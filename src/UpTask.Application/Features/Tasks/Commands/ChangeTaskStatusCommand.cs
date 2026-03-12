using MediatR;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper; // Importante para o TaskMapper
using UpTask.Domain.Enums; // Para reconhecer seus Enums
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Commands
{
    // 1. Use o nome completo ou garanta que o using Domain.Enums resolva a ambiguidade
    public record ChangeTaskStatusCommand(
        Guid TaskId,
        Guid RequesterId,
        UpTask.Domain.Enums.TaskStatus NewStatus) : IRequest<TaskDto>;

    public class ChangeTaskStatusHandler(ITaskRepository repo, IUnitOfWork uow)
        : IRequestHandler<ChangeTaskStatusCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(ChangeTaskStatusCommand cmd, CancellationToken ct)
        {
            var task = await repo.GetByIdAsync(cmd.TaskId, ct)
                ?? throw new NotFoundException("Task", cmd.TaskId);

            // 2. Aqui também: seja explícito na comparação
            if (cmd.NewStatus == UpTask.Domain.Enums.TaskStatus.Completed)
                task.Complete(cmd.RequesterId);
            else
                task.ChangeStatus(cmd.NewStatus);

            await uow.SaveChangesAsync(ct);
            return TaskMapper.MapToDto(task);
        }
    }
}