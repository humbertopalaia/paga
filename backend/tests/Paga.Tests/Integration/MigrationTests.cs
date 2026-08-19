using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Integration tests that verify the initial migration creates the expected schema.
/// </summary>
[Collection("Integration")]
public class MigrationTests
{
    private readonly PostgresFixture _fixture;

    public MigrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrationInicial_DeveCriarCincoTabelas()
    {
        // Arrange
        var expectedTables = new[] { "users", "expense_types", "incomes", "expenses", "refresh_tokens" };

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Act
        var tables = new List<string>();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name != '__EFMigrationsHistory'
            ORDER BY table_name;
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        // Assert
        tables.Should().Contain(expectedTables);
    }

    [Fact]
    public async Task MigrationInicial_DeveCriarIndicesUnicos()
    {
        // Arrange
        var expectedIndexes = new[] { "ix_users_email", "ix_expense_types_user_id_name", "ix_refresh_tokens_token" };

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Act
        var indexes = new List<string>();
        await using var command = new NpgsqlCommand(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public';
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        // Assert
        foreach (var expectedIndex in expectedIndexes)
        {
            indexes.Should().Contain(expectedIndex,
                because: $"the unique index '{expectedIndex}' should exist after migration");
        }
    }

    [Fact]
    public async Task MigrationInicial_DeveCriarForeignKeys()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Act
        var foreignKeys = new List<(string table, string constraint)>();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name, constraint_name
            FROM information_schema.table_constraints
            WHERE table_schema = 'public'
              AND constraint_type = 'FOREIGN KEY'
            ORDER BY table_name, constraint_name;
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            foreignKeys.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert — the tables that must have FK constraints
        var tablesWithFks = foreignKeys.Select(fk => fk.table).Distinct().ToList();
        tablesWithFks.Should().Contain("expense_types", because: "expense_types has FK to users");
        tablesWithFks.Should().Contain("incomes", because: "incomes has FK to users");
        tablesWithFks.Should().Contain("expenses", because: "expenses has FK to users and expense_types");
        tablesWithFks.Should().Contain("refresh_tokens", because: "refresh_tokens has FK to users");

        // expenses must have at least 2 FKs (user + expense_type)
        foreignKeys.Where(fk => fk.table == "expenses").Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
