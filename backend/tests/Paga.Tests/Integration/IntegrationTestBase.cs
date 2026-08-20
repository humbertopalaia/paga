using System.Net.Http.Headers;
using System.Net.Http.Json;
using Paga.Application.DTOs;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Base class for integration tests. Provides a pre-configured <see cref="HttpClient"/>
/// and a helper to authenticate as the seeded admin user.
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgresFixture Fixture;
    protected PagaApiFactory Factory = null!;
    protected HttpClient Client = null!;

    protected IntegrationTestBase(PostgresFixture fixture)
    {
        Fixture = fixture;
    }

    public Task InitializeAsync()
    {
        Factory = new PagaApiFactory(Fixture.ConnectionString);
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Authenticates as the seeded admin user and returns an <see cref="HttpClient"/>
    /// with the Bearer token already configured.
    /// </summary>
    protected async Task<HttpClient> AuthenticateAsync()
    {
        var client = Factory.CreateClient();

        var loginRequest = new
        {
            email = PagaApiFactory.AdminEmail,
            password = PagaApiFactory.AdminPassword
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponse!.AccessToken);

        return client;
    }
}
