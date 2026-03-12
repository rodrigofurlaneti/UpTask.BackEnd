namespace UpTask.Application.Features.Tasks.DTOs
{
    public record ChecklistItemDto(Guid Id, string Description, bool IsCompleted, DateTime? CompletedAt);

}
