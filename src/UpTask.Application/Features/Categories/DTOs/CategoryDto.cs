namespace UpTask.Application.Features.Categories.DTOs
{
    public record CategoryDto(Guid Id, string Name, string? Description, string Color, string? Icon, bool IsGlobal);
}
