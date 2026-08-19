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
                ["Seed:AdminEmail"] = "palaia@increvasenocanal.com",
                ["Seed:AdminPassword"] = "TestAdmin123!",
                ["Serilog:MinimumLevel:Default"] = "Warning"
            });
        });

        builder.UseEnvironment("Testing");
    }
}
