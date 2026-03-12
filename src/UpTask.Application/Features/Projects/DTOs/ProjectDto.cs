using UpTask.Domain.Enums;

namespace UpTask.Application.Features.Projects.DTOs
{
    // ── DTOs ─────────────────────────────────────────────────────────────────────
    public sealed record ProjectDto(
        Guid Id,
        string Name,
        string? Description,
        string Color,
        ProjectStatus Status,
        Priority Priority,
        DateOnly? StartDate,
        DateOnly? PlannedEndDate,
        DateOnly? ActualEndDate,
        int Progress,
        int TotalTasks,
        int CompletedTasks,
        bool IsOwner,
        MemberRole? CurrentUserRole,
        DateTime CreatedAt);
}
