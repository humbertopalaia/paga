namespace Paga.Application.Abstractions;

/// <summary>
/// Provides password hashing and verification capabilities.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plain-text password against a stored hash.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
