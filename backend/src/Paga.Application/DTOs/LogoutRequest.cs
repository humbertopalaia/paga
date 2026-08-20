namespace Paga.Application.DTOs;

/// <summary>
/// Represents the payload for the logout endpoint.
/// </summary>
public record LogoutRequest
{
    /// <summary>
    /// The refresh token to revoke.
    /// </summary>
    public required string RefreshToken { get; init; }
}
