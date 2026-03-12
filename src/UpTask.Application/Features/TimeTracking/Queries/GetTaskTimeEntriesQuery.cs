using MediatR;
using UpTask.Application.Features.TimeTracking.DTOs;
using UpTask.Domain.Interfaces;
using UpTask.Domain.Common; // Para usar o Result

namespace UpTask.Application.Features.TimeTracking.Queries
{
    public record GetTaskTimeEntriesQuery(Guid TaskId, Guid UserId) : IRequest<Result<IEnumerable<TimeEntryDto>>>;

    public class GetTaskTimeEntriesHandler(ITimeEntryRepository repo, ITaskRepository taskRepo)
        : IRequestHandler<GetTaskTimeEntriesQuery, Result<IEnumerable<TimeEntryDto>>>
    {
        public async Task<Result<IEnumerable<TimeEntryDto>>> Handle(GetTaskTimeEntriesQuery q, CancellationToken ct)
        {
            var task = await taskRepo.GetByIdAsync(q.TaskId, ct);

            // Opcional: Validar se a task existe ou se o usuário tem permissão
            if (task is null)
                return Result.Failure<IEnumerable<TimeEntryDto>>(Error.NotFound("Task.NotFound", "Tarefa não encontrada."));

            var entries = await repo.GetByTaskAsync(q.TaskId, ct);

            var dtos = entries.Select(e => new TimeEntryDto(
                e.Id,
                e.TaskId,
                task.Title.Value,
                e.UserId,
                e.StartTime,
                e.EndTime,
                e.DurationMinutes,
                e.Description,
                e.CreatedAt));

            return Result.Success(dtos);
        }
    }
}