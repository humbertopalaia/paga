namespace Paga.Application.Abstractions;

/// <summary>
/// Provides access to the currently authenticated user's identity.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    Guid UserId { get; }
}
