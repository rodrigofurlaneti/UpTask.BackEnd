namespace UpTask.Domain.Common
{

    public sealed record Error(string Code, string Description)
    {
        public static readonly Error None = new(string.Empty, string.Empty);
        public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

        public static Error NotFound(string resource, object id) =>
            new($"{resource}.NotFound", $"{resource} with id '{id}' was not found.");

        public static Error Conflict(string resource, string detail) =>
            new($"{resource}.Conflict", detail);

        public static Error Unauthorized(string detail) =>
            new("Auth.Unauthorized", detail);

        public static Error Validation(string field, string detail) =>
            new($"Validation.{field}", detail);

        public static Error BusinessRule(string code, string detail) =>
            new(code, detail);
    }
}
