using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paga.Application.Abstractions;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Persistence.Seeding;
using Paga.Infrastructure.Security;

namespace Paga.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services: DbContext, password hasher, seeder and seed options.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<PagaDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        return services;
    }
}
