namespace UpTask.Application.Features.Tasks.DTOs
{
    public record ChecklistDto(Guid Id, string Title, int CompletionPercentage,
        IEnumerable<ChecklistItemDto> Items);
}
