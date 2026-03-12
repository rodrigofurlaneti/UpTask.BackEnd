using UpTask.Domain.Common;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities
{
    public sealed class TimeEntry : Entity
    {
        public Guid TaskId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public int DurationMinutes { get; private set; }
        public string? Description { get; private set; }
        public TaskItem? Task { get; private set; }
        public User? User { get; private set; }

        private TimeEntry() { }

        public static TimeEntry Create(Guid taskId, Guid userId, DateTime start, DateTime end, string? description)
        {
            if (end <= start) throw new DomainException("End time must be greater than start time.");

            var duration = (int)(end - start).TotalMinutes;

            return new TimeEntry
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                StartTime = start,
                EndTime = end,
                DurationMinutes = duration,
                Description = description,
                CreatedAt = DateTime.Now, // Padronizando para UTC
                UpdatedAt = DateTime.Now
            };
        }
    }
}