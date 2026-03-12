using UpTask.Domain.Exceptions;
namespace UpTask.Domain.ValueObjects;
public sealed record Email
{
    public string Value { get; }
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email cannot be empty.");
        var normalized = value.Trim().ToLowerInvariant();
        if (!IsValidFormat(normalized))
            throw new DomainException($"'{value}' is not a valid email address.");

        Value = normalized;
    }
    private static bool IsValidFormat(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;
        var domain = email[(atIndex + 1)..];
        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
    }
    public override string ToString() => Value;
}
