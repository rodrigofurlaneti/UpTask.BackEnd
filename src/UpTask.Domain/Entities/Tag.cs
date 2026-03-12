using UpTask.Domain.Common;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities
{
    public sealed class Tag : Entity
    {
        public Guid UserId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Color { get; private set; } = "#9E9E9E";

        private Tag() { }

        public static Tag Create(Guid userId, string name, string color)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tag name is required.");

            return new Tag
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name.Trim().ToLowerInvariant(),
                Color = color,
                CreatedAt = DateTime.Now, 
                UpdatedAt = DateTime.Now
            };
        }
    }
}