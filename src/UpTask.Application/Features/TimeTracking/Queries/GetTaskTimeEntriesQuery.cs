using MediatR;
using UpTask.Application.Features.TimeTracking.DTOs;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.TimeTracking.Queries
{
    public record GetTaskTimeEntriesQuery(Guid TaskId) : IRequest<IEnumerable<TimeEntryDto>>;

    public class GetTaskTimeEntriesHandler(ITimeEntryRepository repo, ITaskRepository taskRepo)
        : IRequestHandler<GetTaskTimeEntriesQuery, IEnumerable<TimeEntryDto>>
    {
        public async Task<IEnumerable<TimeEntryDto>> Handle(GetTaskTimeEntriesQuery q, CancellationToken ct)
        {
            var task = await taskRepo.GetByIdAsync(q.TaskId, ct);
            var entries = await repo.GetByTaskAsync(q.TaskId, ct);

            return entries.Select(e => new TimeEntryDto(
                e.Id,
                e.TaskId,
                task?.Title.Value ?? string.Empty, // CORREÇÃO: .Value adicionado
                e.UserId,
                e.StartTime,
                e.EndTime,
                e.DurationMinutes,
                e.Description,
                e.CreatedAt));
        }
    }
}