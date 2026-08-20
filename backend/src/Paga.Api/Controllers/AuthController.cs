using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paga.Application.Abstractions;
using Paga.Application.DTOs;

namespace Paga.Api.Controllers;

/// <summary>
/// Handles authentication operations: login, token refresh, and logout.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Authenticates a user by email and password, returning a token pair.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A token pair containing access token, refresh token, and expiration.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request.Email, request.Password, ct);
        return Ok(response);
    }

    /// <summary>
    /// Refreshes the token pair using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token to exchange.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A new token pair.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken, ct);
        return Ok(response);
    }

    /// <summary>
    /// Revokes the specified refresh token. Requires authentication.
    /// </summary>
    /// <param name="request">The refresh token to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 200 on success.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(_currentUserService.UserId, request.RefreshToken, ct);
        return Ok();
    }
}
