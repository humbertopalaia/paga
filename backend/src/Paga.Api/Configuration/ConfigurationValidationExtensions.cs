namespace Paga.Api.Configuration;

/// <summary>
/// Extension methods for fail-fast configuration validation at startup.
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Validates that the required connection string is present and non-empty.
    /// Throws <see cref="InvalidOperationException"/> if missing, ensuring the process
    /// fails before accepting traffic.
    /// </summary>
    public static void ValidateConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'ConnectionStrings:Default' é obrigatória. " +
                "Configure via appsettings.Development.json ou variável de ambiente ConnectionStrings__Default.");
        }
    }
}
