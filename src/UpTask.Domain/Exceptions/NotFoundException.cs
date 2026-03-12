namespace UpTask.Domain.Exceptions
{
    public sealed class NotFoundException(string resourceName, object resourceId)
        : Exception($"'{resourceName}' with id '{resourceId}' was not found.");
}
