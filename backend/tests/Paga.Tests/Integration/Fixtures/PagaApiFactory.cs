using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Paga.Tests.Integration.Fixtures;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that overrides configuration
/// to use the ephemeral PostgreSQL container and a known test password.
/// Ensures the test suite never reads appsettings.Development.json.
/// </summary>
public class PagaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    /// <summary>
    /// Known admin email seeded during startup.
    /// </summary>
    public const string AdminEmail = "palaia@increvasenocanal.com";

    /// <summary>
    /// Known admin password used in the test seed.
    /// </summary>
    public const string AdminPassword = "TestAdmin123!";

    /// <summary>
    /// JWT signing key used in tests (at least 32 characters).
    /// </summary>
    public const string JwtKey = "IntegrationTestJwtKeyMustBeAtLeast32Characters!";

    public PagaApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Remove all existing configuration sources so the test suite
            // never reads appsettings.Development.json or environment variables
            // that could point to the development database.
            config.Sources.Clear();

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["Jwt:Key"] = JwtKey,
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
                ["RefreshToken:ExpirationDays"] = "7",
                ["Serilog:MinimumLevel:Default"] = "Warning"
            });
        });

        builder.UseEnvironment("Testing");
    }
}
