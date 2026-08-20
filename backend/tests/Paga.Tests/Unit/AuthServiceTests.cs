using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Paga.Application.Abstractions;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Services;

namespace Paga.Tests.Unit;

public class AuthServiceTests : IDisposable
{
    private readonly PagaDbContext _context;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _sut;

    private const string ValidEmail = "admin@test.com";
    private const string ValidPassword = "Secret123";
    private const string StoredHash = "$2a$11$fakehash";

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<PagaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new PagaDbContext(options);

        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RefreshToken:ExpirationDays"] = "7"
            })
            .Build();

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("test-access-token");
        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

        _sut = new AuthService(
            _context,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            configuration,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Login

    [Fact]
    public async Task LoginAsync_ShouldReturnTokenResponse_WhenCredentialsValid()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Admin", ValidEmail, StoredHash, DateTime.UtcNow);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasherMock
            .Setup(x => x.Verify(ValidPassword, StoredHash))
            .Returns(true);

        // Act
        var result = await _sut.LoginAsync(ValidEmail, ValidPassword);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test-access-token");
        result.RefreshToken.Should().Be("test-refresh-token");
        result.ExpiresIn.Should().Be(1800);

        var persistedToken = await _context.RefreshTokens.FirstOrDefaultAsync();
        persistedToken.Should().NotBeNull();
        persistedToken!.Token.Should().Be("test-refresh-token");
        persistedToken.IsRevoked.Should().BeFalse();
        persistedToken.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowAuthenticationException_WhenEmailNotFound()
    {
        // Arrange — no users seeded

        // Act
        var act = () => _sut.LoginAsync("nonexistent@test.com", ValidPassword);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Credenciais inválidas");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowAuthenticationException_WhenPasswordWrong()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Admin", ValidEmail, StoredHash, DateTime.UtcNow);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasherMock
            .Setup(x => x.Verify("wrong-password", StoredHash))
            .Returns(false);

        // Act
        var act = () => _sut.LoginAsync(ValidEmail, "wrong-password");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Credenciais inválidas");
    }

    #endregion

    #region Refresh

    [Fact]
    public async Task RefreshAsync_ShouldRotateToken_WhenTokenValid()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Admin", ValidEmail, StoredHash, DateTime.UtcNow);
        _context.Users.Add(user);

        var oldToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            "old-refresh-token",
            DateTime.UtcNow.AddDays(7));
        _context.RefreshTokens.Add(oldToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RefreshAsync("old-refresh-token");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test-access-token");
        result.RefreshToken.Should().Be("test-refresh-token");
        result.ExpiresIn.Should().Be(1800);

        // Old token should be revoked
        var revokedToken = await _context.RefreshTokens
            .FirstAsync(t => t.Token == "old-refresh-token");
        revokedToken.IsRevoked.Should().BeTrue();

        // New token should be persisted
        var newToken = await _context.RefreshTokens
            .FirstAsync(t => t.Token == "test-refresh-token");
        newToken.Should().NotBeNull();
        newToken.IsRevoked.Should().BeFalse();
        newToken.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowAuthenticationException_WhenTokenExpired()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Admin", ValidEmail, StoredHash, DateTime.UtcNow);
        _context.Users.Add(user);

        var expiredToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            "expired-token",
            DateTime.UtcNow.AddDays(-1)); // expired yesterday
        _context.RefreshTokens.Add(expiredToken);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _sut.RefreshAsync("expired-token");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Credenciais inválidas");
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowAuthenticationException_WhenTokenRevoked()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Admin", ValidEmail, StoredHash, DateTime.UtcNow);
        _context.Users.Add(user);

        var revokedToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            "revoked-token",
            DateTime.UtcNow.AddDays(7));
        revokedToken.Revoke(); // mark as revoked
        _context.RefreshTokens.Add(revokedToken);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _sut.RefreshAsync("revoked-token");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Credenciais inválidas");
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowAuthenticationException_WhenTokenNotFound()
    {
        // Arrange — no tokens seeded

        // Act
        var act = () => _sut.RefreshAsync("nonexistent-token");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Credenciais inválidas");
    }

    #endregion

    #region Logout

    [Fact]
    public async Task LogoutAsync_ShouldRevokeToken_WhenTokenBelongsToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "Admin", ValidEmail, StoredHash, DateTime.UtcNow);
        _context.Users.Add(user);

        var token = new RefreshToken(
            Guid.NewGuid(),
            userId,
            "valid-refresh-token",
            DateTime.UtcNow.AddDays(7));
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        // Act
        await _sut.LogoutAsync(userId, "valid-refresh-token");

        // Assert
        var revokedToken = await _context.RefreshTokens
            .FirstAsync(t => t.Token == "valid-refresh-token");
        revokedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_ShouldBeIdempotent_WhenTokenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act — no exception should be thrown
        var act = () => _sut.LogoutAsync(userId, "nonexistent-token");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogoutAsync_ShouldBeIdempotent_WhenTokenAlreadyRevoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "Admin", ValidEmail, StoredHash, DateTime.UtcNow);
        _context.Users.Add(user);

        var token = new RefreshToken(
            Guid.NewGuid(),
            userId,
            "already-revoked-token",
            DateTime.UtcNow.AddDays(7));
        token.Revoke();
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _sut.LogoutAsync(userId, "already-revoked-token");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion
}
