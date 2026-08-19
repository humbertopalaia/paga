namespace Paga.Application.Abstractions;

/// <summary>
/// Seeds initial data into the database.
/// </summary>
public interface IDatabaseSeeder
{
    /// <summary>
    /// Seeds required data if the database is empty.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
