namespace Paga.Application.Exceptions;

/// <summary>
/// Thrown when authentication fails due to invalid credentials,
/// expired token, or revoked refresh token.
/// Mapped to HTTP 401 by the global exception handler.
/// </summary>
public class AuthenticationException : DomainException
{
    public AuthenticationException(string message) : base(message) { }
}
