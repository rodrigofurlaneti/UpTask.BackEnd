namespace UpTask.Application.Features.TimeTracking.DTOs
{
    public record TimeEntryDto(Guid Id, Guid TaskId, string TaskTitle, Guid UserId,
        DateTime StartTime, DateTime EndTime, int DurationMinutes, string? Description, DateTime CreatedAt);

}
