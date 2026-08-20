namespace Paga.Application.DTOs;

/// <summary>
/// Represents the payload for the login endpoint.
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User's password in plain text.
    /// </summary>
    public required string Password { get; init; }
}
