using UpTask.Domain.Enums;

namespace UpTask.Application.Features.Projects.DTOs
{
    public sealed record ProjectMemberDto(
        Guid UserId,
        string UserName,
        string Email,
        MemberRole Role,
        DateTime? AcceptedAt);
}
