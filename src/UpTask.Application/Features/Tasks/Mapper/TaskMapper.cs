using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;

namespace UpTask.Application.Features.Tasks.Mapper
{
    internal static class TaskMapper
    {
        internal static TaskDto MapToDto(TaskItem t) => new(
            t.Id,
            t.Title.Value, // Adicionado .Value (TaskTitle para string)
            t.Description,
            (UpTask.Domain.Enums.TaskStatus)t.Status,
            t.Priority,
            t.DueDate,
            t.CompletedAt,
            t.EstimatedHours,
            t.HoursWorked,
            t.StoryPoints,
            t.IsOverdue(),
            t.ProjectId,
            t.Project?.Name.Value, // Adicionado .Value (ProjectName para string)
            t.ParentTaskId,
            t.AssigneeId,
            t.Assignee?.Name,
            t.CreatedAt);
    }
}