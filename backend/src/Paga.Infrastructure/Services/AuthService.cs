using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paga.Application.Abstractions;
using Paga.Application.DTOs;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence;

namespace Paga.Infrastructure.Services;

/// <summary>
/// Implements authentication operations: login, refresh token rotation, and logout.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly PagaDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly int _refreshTokenExpirationDays;
    private readonly ILogger<AuthService> _logger;

    private const int AccessTokenExpiresInSeconds = 1800; // 30 minutes

    public AuthService(
        PagaDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
        _refreshTokenExpirationDays = configuration.GetValue<int>("RefreshToken:ExpirationDays", 7);
    }

    /// <inheritdoc />
    public async Task<TokenResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);

        if (user is null)
        {
            _logger.LogWarning("Login attempt failed: email not found");
            throw new AuthenticationException("Credenciais inválidas");
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Login attempt failed for user {UserId}", user.Id);
            throw new AuthenticationException("Credenciais inválidas");
        }

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(_refreshTokenExpirationDays));

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new TokenResponse(accessToken, refreshTokenValue, AccessTokenExpiresInSeconds);
    }

    /// <inheritdoc />
    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (token is null)
        {
            throw new AuthenticationException("Credenciais inválidas");
        }

        if (token.IsRevoked)
        {
            throw new AuthenticationException("Credenciais inválidas");
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AuthenticationException("Credenciais inválidas");
        }

        // Revoke old token
        token.Revoke();

        // Load user for generating new access token claims
        var user = await _context.Users.FindAsync([token.UserId], ct)
            ?? throw new AuthenticationException("Credenciais inválidas");

        // Generate new token pair
        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            newRefreshTokenValue,
            DateTime.UtcNow.AddDays(_refreshTokenExpirationDays));

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

        return new TokenResponse(newAccessToken, newRefreshTokenValue, AccessTokenExpiresInSeconds);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        // If not found or already revoked, return silently (idempotent)
        if (token is null || token.IsRevoked)
        {
            return;
        }

        // If token belongs to a different user, throw authentication exception
        if (token.UserId != userId)
        {
            throw new AuthenticationException("Credenciais inválidas");
        }

        token.Revoke();
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged out, refresh token revoked", userId);
    }
}
