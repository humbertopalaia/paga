namespace Paga.Domain.Entities;

/// <summary>
/// Opaque refresh token persisted for rotation and revocation.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    /// <summary>
    /// Marks the refresh token as revoked, preventing further use.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }

    // EF Core requires a parameterless constructor; kept private to enforce invariants.
    private RefreshToken()
    {
        Token = string.Empty;
    }
}
