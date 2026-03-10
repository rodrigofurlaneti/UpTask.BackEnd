using FluentValidation;
using MediatR;
using UpTask.Domain.Entities;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.TimeTracking;

public record TimeEntryDto(Guid Id, Guid TaskId, string TaskTitle, Guid UserId,
    DateTime StartTime, DateTime EndTime, int DurationMinutes, string? Description, DateTime CreatedAt);

// ── Log Time ──────────────────────────────────────────────────────────────────
public record LogTimeCommand(Guid TaskId, Guid UserId, DateTime StartTime, DateTime EndTime,
    string? Description) : IRequest<TimeEntryDto>;

public class LogTimeValidator : AbstractValidator<LogTimeCommand>
{
    public LogTimeValidator()
    {
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime).WithMessage("End must be after start.");
        RuleFor(x => x.Description).MaximumLength(300).When(x => x.Description != null);
    }
}

public class LogTimeHandler(ITaskRepository taskRepo, ITimeEntryRepository timeRepo, IUnitOfWork uow)
    : IRequestHandler<LogTimeCommand, TimeEntryDto>
{
    public async Task<TimeEntryDto> Handle(LogTimeCommand cmd, CancellationToken ct)
    {
        var task = await taskRepo.GetByIdAsync(cmd.TaskId, ct)
            ?? throw new NotFoundException("Task", cmd.TaskId);

        var entry = TimeEntry.Create(cmd.TaskId, cmd.UserId, cmd.StartTime, cmd.EndTime, cmd.Description);
        await timeRepo.AddAsync(entry, ct);

        // Recalculate total hours worked
        var totalMinutes = await timeRepo.GetTotalHoursByTaskAsync(cmd.TaskId, ct);
        task.UpdateHoursWorked(totalMinutes);


        await uow.SaveChangesAsync(ct);
        return new TimeEntryDto(entry.Id, task.Id, task.Title, cmd.UserId,
            entry.StartTime, entry.EndTime, entry.DurationMinutes, entry.Description, entry.CreatedAt);
    }
}

// ── Get Task Time Entries ─────────────────────────────────────────────────────
public record GetTaskTimeEntriesQuery(Guid TaskId, Guid RequesterId) : IRequest<IEnumerable<TimeEntryDto>>;

public class GetTaskTimeEntriesHandler(ITaskRepository taskRepo, ITimeEntryRepository timeRepo)
    : IRequestHandler<GetTaskTimeEntriesQuery, IEnumerable<TimeEntryDto>>
{
    public async Task<IEnumerable<TimeEntryDto>> Handle(GetTaskTimeEntriesQuery q, CancellationToken ct)
    {
        var task = await taskRepo.GetByIdAsync(q.TaskId, ct)
            ?? throw new NotFoundException("Task", q.TaskId);

        var entries = await timeRepo.GetByTaskAsync(q.TaskId, ct);
        return entries.Select(e => new TimeEntryDto(e.Id, task.Id, task.Title, e.UserId,
            e.StartTime, e.EndTime, e.DurationMinutes, e.Description, e.CreatedAt));
    }
}

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