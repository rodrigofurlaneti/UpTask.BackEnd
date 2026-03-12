using MediatR;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper;
using UpTask.Domain.Common; // Necessário para o Result
using UpTask.Domain.Enums;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Commands;
public record ChangeTaskStatusCommand(
    Guid TaskId,
    Guid UserId,
    UpTask.Domain.Enums.TaskStatus Status) : IRequest<Result<TaskDto>>;

public class ChangeTaskStatusHandler(ITaskRepository repo, IUnitOfWork uow)
    : IRequestHandler<ChangeTaskStatusCommand, Result<TaskDto>>
{
    public async Task<Result<TaskDto>> Handle(ChangeTaskStatusCommand cmd, CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(cmd.TaskId, ct);

        // Em vez de Exception, usamos o padrão Result para um fluxo mais limpo
        if (task is null)
            return Result.Failure<TaskDto>(Error.NotFound("Task.NotFound", "Tarefa não encontrada."));

        // Regra de Negócio: Se o status for Completed, chama o método específico da Entidade
        if (cmd.Status == UpTask.Domain.Enums.TaskStatus.Completed)
            task.Complete(cmd.UserId);
        else
            task.ChangeStatus(cmd.Status);

        await uow.SaveChangesAsync(ct);

        // Retorna o DTO mapeado dentro do Result.Success
        return Result.Success(TaskMapper.ToDto(task));
    }
}