using UpTask.Domain.Enums;

namespace UpTask.Application.Features.Tasks.DTOs
{
    // Usamos o caminho completo para TaskStatus para evitar conflito com System.Threading.Tasks
    public record TaskDto(
        Guid Id,
        string Title,
        string? Description,
        UpTask.Domain.Enums.TaskStatus Status, // <-- Mudança aqui
        Priority Priority,
        DateTime? DueDate,
        DateTime? CompletedAt,
        decimal? EstimatedHours,
        decimal HoursWorked,
        int? StoryPoints,
        bool IsOverdue,
        Guid? ProjectId,
        string? ProjectName,
        Guid? ParentTaskId,
        Guid? AssigneeId,
        string? AssigneeName,
        DateTime CreatedAt);
}