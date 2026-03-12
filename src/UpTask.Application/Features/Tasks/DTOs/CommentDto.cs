namespace UpTask.Application.Features.Tasks.DTOs
{
    public record CommentDto(Guid Id, string Content, Guid AuthorId, string AuthorName,
        bool IsEdited, DateTime CreatedAt);
}
