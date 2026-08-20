namespace Paga.Application.DTOs;

/// <summary>
/// Payload for creating a new user.
/// </summary>
public record CreateUserRequest
{
    /// <summary>Full name of the user.</summary>
    public required string Name { get; init; }

    /// <summary>Email address (must be unique across the system).</summary>
    public required string Email { get; init; }

    /// <summary>Plain-text password (minimum 6 characters). Stored as BCrypt hash.</summary>
    public required string Password { get; init; }
}
