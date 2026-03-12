using UpTask.Domain.Common;
using UpTask.Domain.Enums;

namespace UpTask.Domain.Entities
{
    public sealed class Notification : Entity
    {
        public Guid UserId { get; private set; }
        public NotificationType Type { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string? Message { get; private set; }
        public string? ReferenceType { get; private set; }
        public Guid? ReferenceId { get; private set; }
        public bool IsRead { get; private set; } = false;
        public DateTime? ReadAt { get; private set; }
        public DateTime? ExpiresAt { get; private set; }

        private Notification() { }

        public static Notification Create(Guid userId, NotificationType type, string title,
            string? message = null, string? referenceType = null, Guid? referenceId = null)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                ExpiresAt = DateTime.UtcNow.AddDays(90),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            Touch();
        }
    }
}