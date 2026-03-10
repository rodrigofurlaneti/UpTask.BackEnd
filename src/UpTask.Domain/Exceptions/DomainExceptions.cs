namespace UpTask.Domain.Exceptions;

public class DomainException(string message) : Exception(message);
public class NotFoundException(string entity, object id)
    : DomainException($"{entity} with id '{id}' was not found.");
public class UnauthorizedException(string message = "Unauthorized access.")
    : DomainException(message);
public class BusinessRuleException(string message) : DomainException(message);
