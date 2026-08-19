using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Paga.Infrastructure.Persistence;

/// <summary>
/// Factory used exclusively by EF Core tooling (dotnet ef migrations, database update).
/// Bypasses the application startup and its fail-fast validation.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PagaDbContext>
{
    public PagaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PagaDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=paga_dev;Username=postgres;Password=postgres")
                      .UseSnakeCaseNamingConvention();

        return new PagaDbContext(optionsBuilder.Options);
    }
}
