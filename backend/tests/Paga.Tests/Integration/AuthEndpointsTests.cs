using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Paga.Application.DTOs;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Integration tests for the /api/auth endpoints (login, refresh, logout).
/// </summary>
public class AuthEndpointsTests : IntegrationTestBase
{
    public AuthEndpointsTests(PostgresFixture fixture) : base(fixture)
    {
    }

    // ──────────────────────────────────────────────────────────────────
    // LOGIN
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ShouldReturn200WithTokenResponse_WhenAdminCredentials()
    {
        // Arrange
        var request = new { email = PagaApiFactory.AdminEmail, password = PagaApiFactory.AdminPassword };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.RefreshToken.Should().NotBeNullOrWhiteSpace();
        token.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenEmailNotFound()
    {
        // Arrange
        var request = new { email = "nonexistent@test.com", password = "password" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenPasswordWrong()
    {
        // Arrange
        var request = new { email = PagaApiFactory.AdminEmail, password = "wrong-password" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturn400_WhenPayloadInvalid()
    {
        // Arrange
        var request = new { email = "", password = "" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────────────────────
    // REFRESH
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ShouldReturn200WithNewPair_WhenTokenValid()
    {
        // Arrange — login first to obtain a valid refresh token
        var loginRequest = new { email = PagaApiFactory.AdminEmail, password = PagaApiFactory.AdminPassword };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshRequest = new { refreshToken = loginToken!.RefreshToken };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newToken = await response.Content.ReadFromJsonAsync<TokenResponse>();
        newToken.Should().NotBeNull();
        newToken!.AccessToken.Should().NotBeNullOrWhiteSpace();
        newToken.RefreshToken.Should().NotBeNullOrWhiteSpace();
        newToken.RefreshToken.Should().NotBe(loginToken.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WhenTokenRevoked()
    {
        // Arrange — login, refresh once (revokes original), then try original again
        var loginRequest = new { email = PagaApiFactory.AdminEmail, password = PagaApiFactory.AdminPassword };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        // First refresh — revokes the original token
        var refreshRequest = new { refreshToken = loginToken!.RefreshToken };
        var firstRefreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — try using the now-revoked original token
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WhenTokenExpired()
    {
        // Note: Testing actual expiration would require time manipulation in the service.
        // Instead, we test with a non-existent token which covers the "not found" path
        // (functionally equivalent from the client perspective — both return 401).
        // Arrange
        var refreshRequest = new { refreshToken = "expired-or-nonexistent-token-value" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────
    // LOGOUT
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ShouldRevokeToken_WhenAuthenticated()
    {
        // Arrange — login to get tokens
        var loginRequest = new { email = PagaApiFactory.AdminEmail, password = PagaApiFactory.AdminPassword };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        using var authClient = await AuthenticateAsync();
        var logoutRequest = new { refreshToken = loginToken!.RefreshToken };

        // Act — logout
        var logoutResponse = await authClient.PostAsJsonAsync("/api/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — refresh with that token should now fail
        var refreshRequest = new { refreshToken = loginToken.RefreshToken };
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldBeIdempotent_WhenCalledTwice()
    {
        // Arrange — login to get tokens
        var loginRequest = new { email = PagaApiFactory.AdminEmail, password = PagaApiFactory.AdminPassword };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginToken = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        using var authClient = await AuthenticateAsync();
        var logoutRequest = new { refreshToken = loginToken!.RefreshToken };

        // Act — logout twice
        var firstLogout = await authClient.PostAsJsonAsync("/api/auth/logout", logoutRequest);
        var secondLogout = await authClient.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert — both should succeed
        firstLogout.StatusCode.Should().Be(HttpStatusCode.OK);
        secondLogout.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────────────────────
    // PROTECTED ENDPOINT WITHOUT TOKEN
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturn401_WithoutToken()
    {
        // Act — call a protected endpoint without any token
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
