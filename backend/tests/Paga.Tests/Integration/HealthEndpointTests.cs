using System.Net;
using FluentAssertions;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Integration tests for the /health endpoint.
/// </summary>
[Collection("Integration")]
public class HealthEndpointTests
{
    private readonly PostgresFixture _fixture;

    public HealthEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetHealth_DeveResponder200_QuandoBancoDisponivel()
    {
        // Arrange
        await using var factory = new PagaApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }
}
