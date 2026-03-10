using System.Text.RegularExpressions;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new BusinessRuleException("Email is required.");
        if (!EmailRegex.IsMatch(value)) throw new BusinessRuleException($"'{value}' is not a valid email address.");
        Value = value.ToLowerInvariant().Trim();
    }

    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}
