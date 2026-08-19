using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Persistence.Seeding;
using Paga.Infrastructure.Security;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Integration tests for the DatabaseSeeder covering all seed scenarios.
/// </summary>
[Collection("Integration")]
public class DatabaseSeederTests
{
    private readonly PostgresFixture _fixture;

    public DatabaseSeederTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private PagaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PagaDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PagaDbContext(options);
    }

    private static DatabaseSeeder CreateSeeder(PagaDbContext context, string adminPassword)
    {
        var hasher = new BcryptPasswordHasher();
        var seedOptions = Options.Create(new SeedOptions
        {
            AdminEmail = "palaia@increvasenocanal.com",
            AdminPassword = adminPassword
        });
        var logger = NullLogger<DatabaseSeeder>.Instance;

        return new DatabaseSeeder(context, hasher, seedOptions, logger);
    }

    private async Task CleanUsersTable()
    {
        await using var context = CreateDbContext();
        context.Users.RemoveRange(await context.Users.ToListAsync());
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SeedAsync_DeveCriarAdministrador_QuandoBaseVaziaComSenha()
    {
        // Arrange
        await CleanUsersTable();
        await using var context = CreateDbContext();
        var seeder = CreateSeeder(context, "TestAdmin123!");

        // Act
        await seeder.SeedAsync();

        // Assert
        await using var verifyContext = CreateDbContext();
        var users = await verifyContext.Users.ToListAsync();
        users.Should().HaveCount(1);
        users[0].Email.Should().Be("palaia@increvasenocanal.com");
        users[0].PasswordHash.Should().NotBeNullOrEmpty();

        // Verify the password hash is valid BCrypt
        var hasher = new BcryptPasswordHasher();
        hasher.Verify("TestAdmin123!", users[0].PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_NaoDeveCriarUsuario_QuandoBaseVaziaSemSenha()
    {
        // Arrange
        await CleanUsersTable();
        await using var context = CreateDbContext();
        var seeder = CreateSeeder(context, "");

        // Act
        await seeder.SeedAsync();

        // Assert
        await using var verifyContext = CreateDbContext();
        var users = await verifyContext.Users.ToListAsync();
        users.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_NaoDeveAlterarNada_QuandoBaseJaPopulada()
    {
        // Arrange
        await CleanUsersTable();

        // Insert a user manually
        await using var setupContext = CreateDbContext();
        var existingUser = new Paga.Domain.Entities.User(
            Guid.NewGuid(),
            "Existing User",
            "existing@example.com",
            "$2a$12$dummyhashvalue1234567890123456789012345678901234567",
            DateTime.UtcNow);
        setupContext.Users.Add(existingUser);
        await setupContext.SaveChangesAsync();

        await using var context = CreateDbContext();
        var seeder = CreateSeeder(context, "TestAdmin123!");

        // Act
        await seeder.SeedAsync();

        // Assert
        await using var verifyContext = CreateDbContext();
        var users = await verifyContext.Users.ToListAsync();
        users.Should().HaveCount(1);
        users[0].Email.Should().Be("existing@example.com");
    }

    [Fact]
    public async Task SeedAsync_NaoDeveDuplicar_QuandoExecutadoRepetidamente()
    {
        // Arrange
        await CleanUsersTable();

        // Act — run the seeder twice
        await using var context1 = CreateDbContext();
        var seeder1 = CreateSeeder(context1, "TestAdmin123!");
        await seeder1.SeedAsync();

        await using var context2 = CreateDbContext();
        var seeder2 = CreateSeeder(context2, "TestAdmin123!");
        await seeder2.SeedAsync();

        // Assert
        await using var verifyContext = CreateDbContext();
        var users = await verifyContext.Users.ToListAsync();
        users.Should().HaveCount(1);
        users[0].Email.Should().Be("palaia@increvasenocanal.com");
    }
}
