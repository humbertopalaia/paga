namespace Paga.Infrastructure.Persistence.Seeding;

/// <summary>
/// Configuration options for the initial admin user seed.
/// Bound to the "Seed" configuration section.
/// </summary>
public class SeedOptions
{
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
