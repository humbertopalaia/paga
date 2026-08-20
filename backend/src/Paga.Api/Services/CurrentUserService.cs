using System.Security.Claims;
using Paga.Application.Abstractions;

namespace Paga.Api.Services;

/// <summary>
/// Provides the authenticated user's identity by extracting claims from the current HTTP context.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="CurrentUserService"/>.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Guid UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
                throw new InvalidOperationException("User identity claim (sub) is missing or invalid.");

            return userId;
        }
    }
}
