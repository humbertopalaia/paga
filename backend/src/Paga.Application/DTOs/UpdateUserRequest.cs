namespace Paga.Application.DTOs;

/// <summary>
/// Payload for updating an existing user. Password is optional — when null or empty, the
/// existing hash is preserved.
/// </summary>
public record UpdateUserRequest
{
    /// <summary>Full name of the user.</summary>
    public required string Name { get; init; }

    /// <summary>Email address (must be unique across the system).</summary>
    public required string Email { get; init; }

    /// <summary>
    /// New plain-text password (minimum 6 characters). When null or empty, the current
    /// password hash remains unchanged.
    /// </summary>
    public string? Password { get; init; }
}
