using UpTask.Domain.Exceptions;
namespace UpTask.Domain.ValueObjects
{
    public sealed record TaskTitle
    {
        public const int MaxLength = 250;
        public string Value { get; }

        public TaskTitle(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Task title cannot be empty.");

            var trimmed = value.Trim();

            if (trimmed.Length > MaxLength)
                throw new DomainException($"Task title cannot exceed {MaxLength} characters.");

            Value = trimmed;
        }

        public override string ToString() => Value;
    }
}
