namespace Paga.Application.Exceptions;

/// <summary>
/// Thrown when an entity is not found or is not accessible by the current user.
/// Mapped to HTTP 404 by the global exception handler.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}
