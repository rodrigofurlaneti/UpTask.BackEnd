using FluentValidation;
using MediatR;
using UpTask.Application.Features.TimeTracking.DTOs;
using UpTask.Domain.Entities;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.TimeTracking.Commands
{
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

            // CORREÇÃO: Usando task.Title.Value para converter o Value Object em string (Erro CS1503)
            return new TimeEntryDto(
                entry.Id,
                task.Id,
                task.Title.Value, // Ajustado aqui
                cmd.UserId,
                entry.StartTime,
                entry.EndTime,
                entry.DurationMinutes,
                entry.Description,
                entry.CreatedAt);
        }
    }
}