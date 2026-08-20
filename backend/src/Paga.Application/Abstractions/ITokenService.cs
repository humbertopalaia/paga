namespace Paga.Application.Abstractions;

/// <summary>
/// Provides JWT access token generation and opaque refresh token generation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token (JWT) for the given user.
    /// </summary>
    string GenerateAccessToken(Guid userId, string email);

    /// <summary>
    /// Generates a cryptographically secure opaque refresh token string.
    /// </summary>
    string GenerateRefreshToken();
}
