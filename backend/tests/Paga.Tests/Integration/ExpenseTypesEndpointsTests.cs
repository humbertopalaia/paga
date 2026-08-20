using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Paga.Application.DTOs;
using Paga.Domain.Entities;
using Paga.Domain.Enums;
using Paga.Infrastructure.Persistence;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Integration tests for the /api/expense-types endpoints.
/// Validates CRUD operations, multi-tenant isolation, conflict handling, and authentication.
/// </summary>
[Collection("Integration")]
public class ExpenseTypesEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExpenseTypesEndpointsTests(PostgresFixture fixture) : base(fixture)
    {
    }

    #region Helpers

    /// <summary>
    /// Creates a second user and returns an authenticated HttpClient for that user.
    /// </summary>
    private async Task<HttpClient> CreateAndAuthenticateSecondUserAsync()
    {
        using var adminClient = await AuthenticateAsync();

        var email = $"user2_{Guid.NewGuid():N}@test.com";
        var password = "SecondUser123!";

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", new
        {
            name = "Second User",
            email,
            password
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var client = Factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponse!.AccessToken);

        return client;
    }

    #endregion

    #region POST /api/expense-types

    [Fact]
    public async Task Create_ShouldReturn201_WhenPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new { name = $"Tipo_{Guid.NewGuid():N}" };

        // Act
        var response = await client.PostAsJsonAsync("/api/expense-types", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be(payload.name);
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenDuplicateNameSameUser()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var name = $"Duplicado_{Guid.NewGuid():N}";

        var firstResponse = await client.PostAsJsonAsync("/api/expense-types", new { name });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act — same name, same user
        var response = await client.PostAsJsonAsync("/api/expense-types", new { name });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenSameNameDifferentUser()
    {
        // Arrange
        var sharedName = $"Compartilhado_{Guid.NewGuid():N}";

        using var adminClient = await AuthenticateAsync();
        var adminResponse = await adminClient.PostAsJsonAsync("/api/expense-types", new { name = sharedName });
        adminResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var userBClient = await CreateAndAuthenticateSecondUserAsync();

        // Act — same name, different user
        var response = await userBClient.PostAsJsonAsync("/api/expense-types", new { name = sharedName });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);
        result!.Name.Should().Be(sharedName);
    }

    #endregion

    #region GET /api/expense-types

    [Fact]
    public async Task GetAll_ShouldReturn200_WithPaginatedListOnlyCurrentUserTypes()
    {
        // Arrange
        using var adminClient = await AuthenticateAsync();
        var uniquePrefix = $"ListA_{Guid.NewGuid():N}";
        await adminClient.PostAsJsonAsync("/api/expense-types", new { name = $"{uniquePrefix}_1" });
        await adminClient.PostAsJsonAsync("/api/expense-types", new { name = $"{uniquePrefix}_2" });

        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        await userBClient.PostAsJsonAsync("/api/expense-types", new { name = $"{uniquePrefix}_Other" });

        // Act
        var response = await adminClient.GetAsync($"/api/expense-types?name={uniquePrefix}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("items", out var items).Should().BeTrue();
        json.TryGetProperty("pageNumber", out _).Should().BeTrue();
        json.TryGetProperty("pageSize", out _).Should().BeTrue();
        json.TryGetProperty("totalCount", out _).Should().BeTrue();
        json.TryGetProperty("totalPages", out _).Should().BeTrue();

        // Admin should only see their own 2 types, not user B's
        items.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithNameFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var uniqueName = $"Filtro_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/expense-types", new { name = uniqueName });
        await client.PostAsJsonAsync("/api/expense-types", new { name = $"Outro_{Guid.NewGuid():N}" });

        // Act — filter by partial name
        var response = await client.GetAsync($"/api/expense-types?name={uniqueName[..10]}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var found = items.EnumerateArray().Any(et =>
            et.GetProperty("name").GetString()!.Contains(uniqueName, StringComparison.OrdinalIgnoreCase));
        found.Should().BeTrue();
    }

    #endregion

    #region GET /api/expense-types/{id}

    [Fact]
    public async Task GetById_ShouldReturn200_WhenOwnType()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var name = $"GetById_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/expense-types", new { name });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        // Act
        var response = await client.GetAsync($"/api/expense-types/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenOtherUsersType()
    {
        // Arrange — create type as admin
        using var adminClient = await AuthenticateAsync();
        var name = $"OtherUser_{Guid.NewGuid():N}";
        var createResponse = await adminClient.PostAsJsonAsync("/api/expense-types", new { name });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        // Act — try to access as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.GetAsync($"/api/expense-types/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenNonExistent()
    {
        // Arrange
        using var client = await AuthenticateAsync();

        // Act
        var response = await client.GetAsync("/api/expense-types/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/expense-types/{id}

    [Fact]
    public async Task Update_ShouldReturn200_WhenPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var originalName = $"BeforeUpdate_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/expense-types", new { name = originalName });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        var updatedName = $"AfterUpdate_{Guid.NewGuid():N}";

        // Act
        var response = await client.PutAsJsonAsync($"/api/expense-types/{created!.Id}", new { name = updatedName });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be(updatedName);
    }

    [Fact]
    public async Task Update_ShouldReturn409_WhenDuplicateName()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var existingName = $"Existente_{Guid.NewGuid():N}";
        var targetName = $"Target_{Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/expense-types", new { name = existingName });
        var createResponse = await client.PostAsJsonAsync("/api/expense-types", new { name = targetName });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        // Act — try to rename target to the existing name
        var response = await client.PutAsJsonAsync($"/api/expense-types/{created!.Id}", new { name = existingName });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenOtherUsersType()
    {
        // Arrange — create type as admin
        using var adminClient = await AuthenticateAsync();
        var name = $"AdminType_{Guid.NewGuid():N}";
        var createResponse = await adminClient.PostAsJsonAsync("/api/expense-types", new { name });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        // Act — try to update as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.PutAsJsonAsync(
            $"/api/expense-types/{created!.Id}",
            new { name = "Hacked" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/expense-types/{id}

    [Fact]
    public async Task Delete_ShouldReturn204_WhenNoLinkedExpenses()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var name = $"ToDelete_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/expense-types", new { name });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        // Act
        var response = await client.DeleteAsync($"/api/expense-types/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await client.GetAsync($"/api/expense-types/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturn409_WhenExpensesExist()
    {
        // Arrange — create an expense type
        using var client = await AuthenticateAsync();
        var name = $"WithExpenses_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/expense-types", new { name });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        // Insert an expense directly via DbContext to simulate linkage
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PagaDbContext>();

        // Get the admin user's ID from the database
        var adminUser = context.Users.First(u => u.Email == PagaApiFactory.AdminEmail);

        var expense = new Expense(
            adminUser.Id,
            DateOnly.FromDateTime(DateTime.Today),
            "Test Expense",
            created!.Id,
            100.00m,
            false,
            null);
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync($"/api/expense-types/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenOtherUsersType()
    {
        // Arrange — create type as admin
        using var adminClient = await AuthenticateAsync();
        var name = $"AdminDelete_{Guid.NewGuid():N}";
        var createResponse = await adminClient.PostAsJsonAsync("/api/expense-types", new { name });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>(JsonOptions);

        // Act — try to delete as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.DeleteAsync($"/api/expense-types/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authentication required (401)

    [Fact]
    public async Task AllEndpoints_ShouldReturn401_WithoutToken()
    {
        // Arrange — use unauthenticated client (Client from IntegrationTestBase)

        // Act & Assert — GET list
        var getAll = await Client.GetAsync("/api/expense-types");
        getAll.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // GET by id
        var getById = await Client.GetAsync("/api/expense-types/1");
        getById.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // POST
        var post = await Client.PostAsJsonAsync("/api/expense-types", new { name = "Unauthorized" });
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // PUT
        var put = await Client.PutAsJsonAsync("/api/expense-types/1", new { name = "Unauthorized" });
        put.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // DELETE
        var delete = await Client.DeleteAsync("/api/expense-types/1");
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
