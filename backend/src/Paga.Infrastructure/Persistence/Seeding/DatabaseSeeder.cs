using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paga.Application.Abstractions;
using Paga.Domain.Entities;

namespace Paga.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the admin user conditionally on application startup.
/// </summary>
public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly PagaDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SeedOptions _options;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        PagaDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var hasUsers = await _context.Users.AnyAsync(cancellationToken);

        if (hasUsers)
        {
            _logger.LogInformation("Seed skipped, database already has users");
            return;
        }

        if (string.IsNullOrEmpty(_options.AdminPassword))
        {
            _logger.LogWarning("Seed skipped, AdminPassword not configured");
            return;
        }

        var passwordHash = _passwordHasher.Hash(_options.AdminPassword);

        var admin = new User(
            Guid.NewGuid(),
            "Administrador",
            _options.AdminEmail,
            passwordHash,
            DateTime.UtcNow);

        _context.Users.Add(admin);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Admin user created with email {Email}", _options.AdminEmail);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogInformation("Admin user was already created concurrently");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        // Npgsql wraps PostgreSQL errors; unique_violation has code 23505.
        // Also handle generic unique constraint messages for resilience.
        if (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            return true;
        }

        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
    }
}
