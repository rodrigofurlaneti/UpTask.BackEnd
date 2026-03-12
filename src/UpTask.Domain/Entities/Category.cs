using UpTask.Domain.Common;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities
{
    public sealed class Category : Entity
    {
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public string Color { get; private set; } = "#607D8B";
        public string? Icon { get; private set; }
        public Guid? ParentCategoryId { get; private set; }
        public Guid? UserId { get; private set; } // null = global

        private Category() { }

        public static Category Create(string name, string? description, string color,
            string? icon, Guid? userId, Guid? parentId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required.");

            return new Category
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Description = description,
                Color = color,
                Icon = icon,
                UserId = userId,
                ParentCategoryId = parentId,
                CreatedAt = DateTime.UtcNow, // Padrão recomendado
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void Update(string name, string? description, string color, string? icon)
        {
            Name = name.Trim();
            Description = description;
            Color = color;
            Icon = icon;

            // ALTERADO: Usando o método da nova classe Entity
            Touch();
        }

        public bool IsGlobal => UserId == null;
    }
}