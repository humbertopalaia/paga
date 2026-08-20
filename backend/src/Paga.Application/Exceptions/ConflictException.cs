namespace Paga.Application.Exceptions;

/// <summary>
/// Thrown when a business rule conflict occurs, such as a duplicate email
/// or an attempt to delete a referenced entity.
/// Mapped to HTTP 409 by the global exception handler.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
