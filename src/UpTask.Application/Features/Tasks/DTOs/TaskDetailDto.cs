namespace UpTask.Application.Features.Tasks.DTOs
{
    public record TaskDetailDto(
        TaskDto Task,
        IEnumerable<TaskDto> SubTasks,
        IEnumerable<CommentDto> Comments,
        IEnumerable<ChecklistDto> Checklists);
}
