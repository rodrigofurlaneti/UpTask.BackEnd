using UpTask.Domain.Exceptions;
namespace UpTask.Domain.ValueObjects
{
    public sealed record ProjectName
    {
        public const int MaxLength = 150;
        public string Value { get; }

        public ProjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Project name cannot be empty.");

            var trimmed = value.Trim();

            if (trimmed.Length > MaxLength)
                throw new DomainException($"Project name cannot exceed {MaxLength} characters.");

            Value = trimmed;
        }

        public override string ToString() => Value;
    }
}
