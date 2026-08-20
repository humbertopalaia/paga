namespace Paga.Application.Exceptions;

/// <summary>
/// Base class for domain-level exceptions mapped by the global exception handler
/// to appropriate HTTP status codes and ProblemDetails responses.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
