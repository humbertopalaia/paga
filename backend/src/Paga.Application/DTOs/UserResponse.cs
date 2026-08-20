namespace Paga.Application.DTOs;

/// <summary>
/// Public representation of a user. Never exposes passwordHash.
/// </summary>
public record UserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
