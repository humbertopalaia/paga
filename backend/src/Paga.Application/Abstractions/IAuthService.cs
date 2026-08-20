using Paga.Application.DTOs;

namespace Paga.Application.Abstractions;

/// <summary>
/// Provides authentication operations: login, refresh token rotation, and logout.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user by email and password, returning a token pair.
    /// </summary>
    Task<TokenResponse> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Rotates a refresh token, revoking the old one and issuing a new token pair.
    /// </summary>
    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Revokes the specified refresh token for the given user. Idempotent.
    /// </summary>
    Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default);
}
