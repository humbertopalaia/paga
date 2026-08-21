using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Paga.Application.DTOs;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration.Incomes;

/// <summary>
/// Integration tests for the /api/incomes endpoints.
/// Validates CRUD operations, filtering, pagination, multi-tenant isolation, and authentication.
/// </summary>
[Collection("Integration")]
public class IncomesEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IncomesEndpointsTests(PostgresFixture fixture) : base(fixture)
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

    /// <summary>
    /// Creates an income via API and returns the response.
    /// </summary>
    private async Task<IncomeResponse> CreateIncomeAsync(HttpClient client, object? payload = null)
    {
        payload ??= new
        {
            date = "2024-06-15",
            description = $"Receita_{Guid.NewGuid():N}",
            value = 5000.00m,
            isRecurring = false,
            frequency = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/incomes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<IncomeResponse>(JsonOptions);
        return result!;
    }

    #endregion

    #region POST /api/incomes

    [Fact]
    public async Task Create_ShouldReturn201_WhenNonRecurringPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            date = "2024-06-15",
            description = "Salário",
            value = 8500.50m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/incomes", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<IncomeResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.Date.Should().Be("2024-06-15");
        result.Description.Should().Be("Salário");
        result.Value.Should().Be(8500.50m);
        result.IsRecurring.Should().BeFalse();
        result.Frequency.Should().BeNull();
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenRecurringPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            date = "2024-01-01",
            description = "Aluguel recebido",
            value = 3200.00m,
            isRecurring = true,
            frequency = "monthly"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/incomes", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<IncomeResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("monthly");
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenRecurringWithoutFrequency()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            date = "2024-06-15",
            description = "Receita inválida",
            value = 1000.00m,
            isRecurring = true,
            frequency = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/incomes", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenValueZeroOrNegative()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payloadZero = new
        {
            date = "2024-06-15",
            description = "Valor zero",
            value = 0m,
            isRecurring = false,
            frequency = (string?)null
        };
        var payloadNegative = new
        {
            date = "2024-06-15",
            description = "Valor negativo",
            value = -100m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act & Assert
        var responseZero = await client.PostAsJsonAsync("/api/incomes", payloadZero);
        responseZero.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseNegative = await client.PostAsJsonAsync("/api/incomes", payloadNegative);
        responseNegative.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenDescriptionMissing()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            date = "2024-06-15",
            description = "",
            value = 1000.00m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/incomes", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/incomes (list with filters)

    [Fact]
    public async Task GetAll_ShouldReturn200_WithPaginatedListOnlyCurrentUserIncomes()
    {
        // Arrange
        using var adminClient = await AuthenticateAsync();
        var uniqueDesc = $"List_{Guid.NewGuid():N}";
        await CreateIncomeAsync(adminClient, new
        {
            date = "2024-03-01",
            description = $"{uniqueDesc}_admin1",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateIncomeAsync(adminClient, new
        {
            date = "2024-03-02",
            description = $"{uniqueDesc}_admin2",
            value = 2000m,
            isRecurring = false,
            frequency = (string?)null
        });

        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        await CreateIncomeAsync(userBClient, new
        {
            date = "2024-03-01",
            description = $"{uniqueDesc}_userB",
            value = 500m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act
        var response = await adminClient.GetAsync($"/api/incomes?description={uniqueDesc}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("items", out var items).Should().BeTrue();
        json.TryGetProperty("pageNumber", out _).Should().BeTrue();
        json.TryGetProperty("pageSize", out _).Should().BeTrue();
        json.TryGetProperty("totalCount", out var totalCount).Should().BeTrue();
        json.TryGetProperty("totalPages", out _).Should().BeTrue();

        // Admin should only see their own 2 incomes, not user B's
        totalCount.GetInt32().Should().Be(2);
        items.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDateFromFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var uniqueDesc = $"DateFrom_{Guid.NewGuid():N}";
        await CreateIncomeAsync(client, new
        {
            date = "2024-01-15",
            description = $"{uniqueDesc}_old",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateIncomeAsync(client, new
        {
            date = "2024-06-15",
            description = $"{uniqueDesc}_new",
            value = 2000m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter from June
        var response = await client.GetAsync($"/api/incomes?description={uniqueDesc}&dateFrom=2024-06-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("description").GetString().Should().Contain("_new");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDateToFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var uniqueDesc = $"DateTo_{Guid.NewGuid():N}";
        await CreateIncomeAsync(client, new
        {
            date = "2024-01-15",
            description = $"{uniqueDesc}_old",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateIncomeAsync(client, new
        {
            date = "2024-06-15",
            description = $"{uniqueDesc}_new",
            value = 2000m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter up to January
        var response = await client.GetAsync($"/api/incomes?description={uniqueDesc}&dateTo=2024-01-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("description").GetString().Should().Contain("_old");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDateRangeFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var uniqueDesc = $"Range_{Guid.NewGuid():N}";
        await CreateIncomeAsync(client, new
        {
            date = "2024-01-15",
            description = $"{uniqueDesc}_jan",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateIncomeAsync(client, new
        {
            date = "2024-03-15",
            description = $"{uniqueDesc}_mar",
            value = 2000m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateIncomeAsync(client, new
        {
            date = "2024-06-15",
            description = $"{uniqueDesc}_jun",
            value = 3000m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter Feb-Apr
        var response = await client.GetAsync(
            $"/api/incomes?description={uniqueDesc}&dateFrom=2024-02-01&dateTo=2024-04-30");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("description").GetString().Should().Contain("_mar");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDescriptionFilterCaseInsensitive()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var uniqueDesc = $"CaseTest_{Guid.NewGuid():N}";
        await CreateIncomeAsync(client, new
        {
            date = "2024-06-15",
            description = uniqueDesc,
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter with different case
        var response = await client.GetAsync($"/api/incomes?description={uniqueDesc.ToUpper()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithIsRecurringFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var uniqueDesc = $"Recurring_{Guid.NewGuid():N}";
        await CreateIncomeAsync(client, new
        {
            date = "2024-06-01",
            description = $"{uniqueDesc}_recur",
            value = 1000m,
            isRecurring = true,
            frequency = "weekly"
        });
        await CreateIncomeAsync(client, new
        {
            date = "2024-06-02",
            description = $"{uniqueDesc}_single",
            value = 2000m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter only recurring
        var response = await client.GetAsync($"/api/incomes?description={uniqueDesc}&isRecurring=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("isRecurring").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region GET /api/incomes/{id}

    [Fact]
    public async Task GetById_ShouldReturn200_WhenOwnIncome()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var created = await CreateIncomeAsync(client, new
        {
            date = "2024-07-10",
            description = "Receita GetById",
            value = 4500.00m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act
        var response = await client.GetAsync($"/api/incomes/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Description.Should().Be("Receita GetById");
        result.Value.Should().Be(4500.00m);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenOtherUsersIncome()
    {
        // Arrange — create income as admin
        using var adminClient = await AuthenticateAsync();
        var created = await CreateIncomeAsync(adminClient);

        // Act — try to access as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.GetAsync($"/api/incomes/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenNonExistent()
    {
        // Arrange
        using var client = await AuthenticateAsync();

        // Act
        var response = await client.GetAsync("/api/incomes/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/incomes/{id}

    [Fact]
    public async Task Update_ShouldReturn200_WhenPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var created = await CreateIncomeAsync(client, new
        {
            date = "2024-06-01",
            description = "Before update",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });

        var updatePayload = new
        {
            date = "2024-07-01",
            description = "After update",
            value = 2500.50m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/incomes/{created.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeResponse>(JsonOptions);
        result!.Date.Should().Be("2024-07-01");
        result.Description.Should().Be("After update");
        result.Value.Should().Be(2500.50m);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WhenTogglingRecurrenceOn()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var created = await CreateIncomeAsync(client, new
        {
            date = "2024-06-01",
            description = "Non-recurring",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });

        var updatePayload = new
        {
            date = "2024-06-01",
            description = "Now recurring",
            value = 1000m,
            isRecurring = true,
            frequency = "monthly"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/incomes/{created.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeResponse>(JsonOptions);
        result!.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("monthly");
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenOtherUsersIncome()
    {
        // Arrange — create income as admin
        using var adminClient = await AuthenticateAsync();
        var created = await CreateIncomeAsync(adminClient);

        // Act — try to update as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.PutAsJsonAsync($"/api/incomes/{created.Id}", new
        {
            date = "2024-06-15",
            description = "Hacked",
            value = 9999m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenNotRecurringWithFrequency()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var created = await CreateIncomeAsync(client);

        var updatePayload = new
        {
            date = "2024-06-15",
            description = "Invalid combo",
            value = 1000m,
            isRecurring = false,
            frequency = "monthly"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/incomes/{created.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE /api/incomes/{id}

    [Fact]
    public async Task Delete_ShouldReturn204_WhenOwnIncome()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var created = await CreateIncomeAsync(client);

        // Act
        var response = await client.DeleteAsync($"/api/incomes/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await client.GetAsync($"/api/incomes/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenOtherUsersIncome()
    {
        // Arrange — create income as admin
        using var adminClient = await AuthenticateAsync();
        var created = await CreateIncomeAsync(adminClient);

        // Act — try to delete as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.DeleteAsync($"/api/incomes/{created.Id}");

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
        var getAll = await Client.GetAsync("/api/incomes");
        getAll.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // GET by id
        var getById = await Client.GetAsync("/api/incomes/1");
        getById.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // POST
        var post = await Client.PostAsJsonAsync("/api/incomes", new
        {
            date = "2024-06-15",
            description = "Unauthorized",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // PUT
        var put = await Client.PutAsJsonAsync("/api/incomes/1", new
        {
            date = "2024-06-15",
            description = "Unauthorized",
            value = 1000m,
            isRecurring = false,
            frequency = (string?)null
        });
        put.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // DELETE
        var delete = await Client.DeleteAsync("/api/incomes/1");
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Multi-tenant isolation

    [Fact]
    public async Task Isolation_UserACannotReadUpdateOrDeleteUserBIncome()
    {
        // Arrange — create income as user B
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var incomeB = await CreateIncomeAsync(userBClient, new
        {
            date = "2024-06-15",
            description = "User B income",
            value = 7500m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act & Assert — admin (user A) cannot read user B's income
        using var adminClient = await AuthenticateAsync();

        var getResponse = await adminClient.GetAsync($"/api/incomes/{incomeB.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Admin cannot update user B's income
        var putResponse = await adminClient.PutAsJsonAsync($"/api/incomes/{incomeB.Id}", new
        {
            date = "2024-06-15",
            description = "Hacked by admin",
            value = 1m,
            isRecurring = false,
            frequency = (string?)null
        });
        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Admin cannot delete user B's income
        var deleteResponse = await adminClient.DeleteAsync($"/api/incomes/{incomeB.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
