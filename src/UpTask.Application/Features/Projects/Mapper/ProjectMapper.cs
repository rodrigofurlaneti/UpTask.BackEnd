using UpTask.Domain.Entities;
using UpTask.Application.Features.Projects.DTOs;

namespace UpTask.Application.Features.Projects.Mapper
{
    internal static class ProjectMapper
    {
        internal static ProjectDto ToDto(Project p, int total, int completed, Guid currentUserId) =>
            new(
                p.Id,
                p.Name.Value,
                p.Description,
                p.Color.Value,
                p.Status,
                p.Priority,
                p.StartDate,
                p.PlannedEndDate,
                p.ActualEndDate,
                p.Progress,
                total,
                completed,
                p.IsOwner(currentUserId),
                p.GetMemberRole(currentUserId),
                p.CreatedAt);
        internal static ProjectDto MapToDto(Project p, int total, int completed, Guid currentUserId)
            => ToDto(p, total, completed, currentUserId);
    }
}