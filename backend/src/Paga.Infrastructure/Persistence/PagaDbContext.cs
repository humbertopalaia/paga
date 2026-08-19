using Microsoft.EntityFrameworkCore;
using Paga.Domain.Entities;

namespace Paga.Infrastructure.Persistence;

/// <summary>
/// Main database context for the PAGA application.
/// </summary>
public class PagaDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public PagaDbContext(DbContextOptions<PagaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PagaDbContext).Assembly);
    }
}
