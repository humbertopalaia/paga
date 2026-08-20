namespace Paga.Application.DTOs;

/// <summary>
/// Represents the payload for the refresh token endpoint.
/// </summary>
public record RefreshRequest
{
    /// <summary>
    /// The refresh token to exchange for a new token pair.
    /// </summary>
    public required string RefreshToken { get; init; }
}
