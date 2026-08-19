using Microsoft.EntityFrameworkCore;
using Paga.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Paga.Tests.Integration.Fixtures;

/// <summary>
/// Starts a PostgreSQL container via Testcontainers, applies migrations and
/// exposes the connection string for integration tests.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    /// <summary>
    /// Connection string pointing to the ephemeral container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to start PostgreSQL container. Ensure Docker is running and accessible.", ex);
        }

        var options = new DbContextOptionsBuilder<PagaDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new PagaDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}
