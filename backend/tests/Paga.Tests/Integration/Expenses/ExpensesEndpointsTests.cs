using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Paga.Application.DTOs;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration.Expenses;

/// <summary>
/// Integration tests for the /api/expenses endpoints.
/// Validates CRUD operations, filtering, pagination, multi-tenant isolation,
/// ExpenseType ownership validation, referential integrity, and authentication.
/// </summary>
[Collection("Integration")]
public class ExpensesEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExpensesEndpointsTests(PostgresFixture fixture) : base(fixture)
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
    /// Creates an expense type via API and returns the response as JsonElement.
    /// </summary>
    private async Task<JsonElement> CreateExpenseTypeAsync(HttpClient client, string name = "Alimentação")
    {
        var uniqueName = $"{name}_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/expense-types", new { name = uniqueName });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    /// <summary>
    /// Creates an expense via API and returns the response as JsonElement.
    /// </summary>
    private async Task<JsonElement> CreateExpenseAsync(HttpClient client, int expenseTypeId, object? payload = null)
    {
        payload ??= new
        {
            dueDate = "2024-06-15",
            description = $"Despesa_{Guid.NewGuid():N}",
            expenseTypeId,
            value = 150.00m,
            isRecurring = false,
            frequency = (string?)null
        };
        var response = await client.PostAsJsonAsync("/api/expenses", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    #endregion

    #region POST /api/expenses

    [Fact]
    public async Task Create_ShouldReturn201_WhenNonRecurringPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "Transporte");
        var typeId = type.GetProperty("id").GetInt32();
        var typeName = type.GetProperty("name").GetString();

        var payload = new
        {
            dueDate = "2024-06-15",
            description = "Uber para reunião",
            expenseTypeId = typeId,
            value = 45.90m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/expenses", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.DueDate.Should().Be("2024-06-15");
        result.Description.Should().Be("Uber para reunião");
        result.ExpenseTypeId.Should().Be(typeId);
        result.ExpenseTypeName.Should().Be(typeName);
        result.Value.Should().Be(45.90m);
        result.IsRecurring.Should().BeFalse();
        result.Frequency.Should().BeNull();
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenRecurringPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "Assinatura");
        var typeId = type.GetProperty("id").GetInt32();

        var payload = new
        {
            dueDate = "2024-01-10",
            description = "Netflix",
            expenseTypeId = typeId,
            value = 55.90m,
            isRecurring = true,
            frequency = "monthly"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/expenses", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("monthly");
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenRecurringWithoutFrequency()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client);
        var typeId = type.GetProperty("id").GetInt32();

        var payload = new
        {
            dueDate = "2024-06-15",
            description = "Despesa inválida",
            expenseTypeId = typeId,
            value = 100.00m,
            isRecurring = true,
            frequency = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/expenses", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenValueZeroOrNegative()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client);
        var typeId = type.GetProperty("id").GetInt32();

        var payloadZero = new
        {
            dueDate = "2024-06-15",
            description = "Valor zero",
            expenseTypeId = typeId,
            value = 0m,
            isRecurring = false,
            frequency = (string?)null
        };
        var payloadNegative = new
        {
            dueDate = "2024-06-15",
            description = "Valor negativo",
            expenseTypeId = typeId,
            value = -50m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act & Assert
        var responseZero = await client.PostAsJsonAsync("/api/expenses", payloadZero);
        responseZero.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseNegative = await client.PostAsJsonAsync("/api/expenses", payloadNegative);
        responseNegative.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenDescriptionMissing()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client);
        var typeId = type.GetProperty("id").GetInt32();

        var payload = new
        {
            dueDate = "2024-06-15",
            description = "",
            expenseTypeId = typeId,
            value = 100.00m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/expenses", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenExpenseTypeIdBelongsToOtherUser()
    {
        // Arrange — create type as admin
        using var adminClient = await AuthenticateAsync();
        var adminType = await CreateExpenseTypeAsync(adminClient, "Tipo do Admin");
        var adminTypeId = adminType.GetProperty("id").GetInt32();

        // Act — try to create expense as user B using admin's type
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var payload = new
        {
            dueDate = "2024-06-15",
            description = "Despesa com tipo alheio",
            expenseTypeId = adminTypeId,
            value = 200.00m,
            isRecurring = false,
            frequency = (string?)null
        };
        var response = await userBClient.PostAsJsonAsync("/api/expenses", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenExpenseTypeIdNonExistent()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            dueDate = "2024-06-15",
            description = "Despesa com tipo inexistente",
            expenseTypeId = 999999,
            value = 100.00m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/expenses", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/expenses (list with filters)

    [Fact]
    public async Task GetAll_ShouldReturn200_WithPaginatedListOnlyCurrentUserExpenses()
    {
        // Arrange
        using var adminClient = await AuthenticateAsync();
        var adminType = await CreateExpenseTypeAsync(adminClient, "AdminList");
        var adminTypeId = adminType.GetProperty("id").GetInt32();

        var uniqueDesc = $"List_{Guid.NewGuid():N}";
        await CreateExpenseAsync(adminClient, adminTypeId, new
        {
            dueDate = "2024-03-01",
            description = $"{uniqueDesc}_admin1",
            expenseTypeId = adminTypeId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateExpenseAsync(adminClient, adminTypeId, new
        {
            dueDate = "2024-03-02",
            description = $"{uniqueDesc}_admin2",
            expenseTypeId = adminTypeId,
            value = 200m,
            isRecurring = false,
            frequency = (string?)null
        });

        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var userBType = await CreateExpenseTypeAsync(userBClient, "UserBList");
        var userBTypeId = userBType.GetProperty("id").GetInt32();
        await CreateExpenseAsync(userBClient, userBTypeId, new
        {
            dueDate = "2024-03-01",
            description = $"{uniqueDesc}_userB",
            expenseTypeId = userBTypeId,
            value = 50m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act
        var response = await adminClient.GetAsync($"/api/expenses?description={uniqueDesc}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("items", out var items).Should().BeTrue();
        json.TryGetProperty("pageNumber", out _).Should().BeTrue();
        json.TryGetProperty("pageSize", out _).Should().BeTrue();
        json.TryGetProperty("totalCount", out var totalCount).Should().BeTrue();
        json.TryGetProperty("totalPages", out _).Should().BeTrue();

        // Admin should only see their own 2 expenses, not user B's
        totalCount.GetInt32().Should().Be(2);
        items.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDueDateFromFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "DateFrom");
        var typeId = type.GetProperty("id").GetInt32();

        var uniqueDesc = $"DueDateFrom_{Guid.NewGuid():N}";
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-01-15",
            description = $"{uniqueDesc}_old",
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-06-15",
            description = $"{uniqueDesc}_new",
            expenseTypeId = typeId,
            value = 200m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter from June
        var response = await client.GetAsync(
            $"/api/expenses?description={uniqueDesc}&dueDateFrom=2024-06-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("description").GetString().Should().Contain("_new");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDueDateToFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "DateTo");
        var typeId = type.GetProperty("id").GetInt32();

        var uniqueDesc = $"DueDateTo_{Guid.NewGuid():N}";
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-01-15",
            description = $"{uniqueDesc}_old",
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-06-15",
            description = $"{uniqueDesc}_new",
            expenseTypeId = typeId,
            value = 200m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter up to January
        var response = await client.GetAsync(
            $"/api/expenses?description={uniqueDesc}&dueDateTo=2024-01-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("description").GetString().Should().Contain("_old");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDueDateRangeFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "DateRange");
        var typeId = type.GetProperty("id").GetInt32();

        var uniqueDesc = $"Range_{Guid.NewGuid():N}";
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-01-15",
            description = $"{uniqueDesc}_jan",
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-03-15",
            description = $"{uniqueDesc}_mar",
            expenseTypeId = typeId,
            value = 200m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-06-15",
            description = $"{uniqueDesc}_jun",
            expenseTypeId = typeId,
            value = 300m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter Feb-Apr
        var response = await client.GetAsync(
            $"/api/expenses?description={uniqueDesc}&dueDateFrom=2024-02-01&dueDateTo=2024-04-30");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("description").GetString().Should().Contain("_mar");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithExpenseTypeIdFilter()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var typeA = await CreateExpenseTypeAsync(client, "TipoA");
        var typeAId = typeA.GetProperty("id").GetInt32();
        var typeB = await CreateExpenseTypeAsync(client, "TipoB");
        var typeBId = typeB.GetProperty("id").GetInt32();

        var uniqueDesc = $"TypeFilter_{Guid.NewGuid():N}";
        await CreateExpenseAsync(client, typeAId, new
        {
            dueDate = "2024-06-01",
            description = $"{uniqueDesc}_A",
            expenseTypeId = typeAId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        await CreateExpenseAsync(client, typeBId, new
        {
            dueDate = "2024-06-02",
            description = $"{uniqueDesc}_B",
            expenseTypeId = typeBId,
            value = 200m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter by type A
        var response = await client.GetAsync(
            $"/api/expenses?description={uniqueDesc}&expenseTypeId={typeAId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("expenseTypeId").GetInt32().Should().Be(typeAId);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200_WithDescriptionFilterCaseInsensitive()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "CaseTest");
        var typeId = type.GetProperty("id").GetInt32();

        var uniqueDesc = $"CaseTest_{Guid.NewGuid():N}";
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-06-15",
            description = uniqueDesc,
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter with different case
        var response = await client.GetAsync($"/api/expenses?description={uniqueDesc.ToUpper()}");

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
        var type = await CreateExpenseTypeAsync(client, "RecurFilter");
        var typeId = type.GetProperty("id").GetInt32();

        var uniqueDesc = $"Recurring_{Guid.NewGuid():N}";
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-06-01",
            description = $"{uniqueDesc}_recur",
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = true,
            frequency = "weekly"
        });
        await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-06-02",
            description = $"{uniqueDesc}_single",
            expenseTypeId = typeId,
            value = 200m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Act — filter only recurring
        var response = await client.GetAsync(
            $"/api/expenses?description={uniqueDesc}&isRecurring=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("isRecurring").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region GET /api/expenses/{id}

    [Fact]
    public async Task GetById_ShouldReturn200_WhenOwnExpense()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "GetById");
        var typeId = type.GetProperty("id").GetInt32();
        var typeName = type.GetProperty("name").GetString();

        var created = await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-07-10",
            description = "Despesa GetById",
            expenseTypeId = typeId,
            value = 350.00m,
            isRecurring = false,
            frequency = (string?)null
        });
        var createdId = created.GetProperty("id").GetInt32();

        // Act
        var response = await client.GetAsync($"/api/expenses/{createdId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdId);
        result.Description.Should().Be("Despesa GetById");
        result.Value.Should().Be(350.00m);
        result.ExpenseTypeId.Should().Be(typeId);
        result.ExpenseTypeName.Should().Be(typeName);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenOtherUsersExpense()
    {
        // Arrange — create expense as admin
        using var adminClient = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(adminClient, "OtherUser");
        var typeId = type.GetProperty("id").GetInt32();
        var created = await CreateExpenseAsync(adminClient, typeId);
        var createdId = created.GetProperty("id").GetInt32();

        // Act — try to access as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.GetAsync($"/api/expenses/{createdId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenNonExistent()
    {
        // Arrange
        using var client = await AuthenticateAsync();

        // Act
        var response = await client.GetAsync("/api/expenses/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/expenses/{id}

    [Fact]
    public async Task Update_ShouldReturn200_WhenPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var typeA = await CreateExpenseTypeAsync(client, "OriginalType");
        var typeAId = typeA.GetProperty("id").GetInt32();
        var typeB = await CreateExpenseTypeAsync(client, "NewType");
        var typeBId = typeB.GetProperty("id").GetInt32();
        var typeBName = typeB.GetProperty("name").GetString();

        var created = await CreateExpenseAsync(client, typeAId, new
        {
            dueDate = "2024-06-01",
            description = "Before update",
            expenseTypeId = typeAId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        var createdId = created.GetProperty("id").GetInt32();

        var updatePayload = new
        {
            dueDate = "2024-07-01",
            description = "After update",
            expenseTypeId = typeBId,
            value = 250.50m,
            isRecurring = false,
            frequency = (string?)null
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/expenses/{createdId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        result!.DueDate.Should().Be("2024-07-01");
        result.Description.Should().Be("After update");
        result.ExpenseTypeId.Should().Be(typeBId);
        result.ExpenseTypeName.Should().Be(typeBName);
        result.Value.Should().Be(250.50m);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WhenTogglingRecurrenceOn()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "ToggleRecur");
        var typeId = type.GetProperty("id").GetInt32();

        var created = await CreateExpenseAsync(client, typeId, new
        {
            dueDate = "2024-06-01",
            description = "Non-recurring",
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        var createdId = created.GetProperty("id").GetInt32();

        var updatePayload = new
        {
            dueDate = "2024-06-01",
            description = "Now recurring",
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = true,
            frequency = "monthly"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/expenses/{createdId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        result!.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("monthly");
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenOtherUsersExpense()
    {
        // Arrange — create expense as admin
        using var adminClient = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(adminClient, "AdminUpdate");
        var typeId = type.GetProperty("id").GetInt32();
        var created = await CreateExpenseAsync(adminClient, typeId);
        var createdId = created.GetProperty("id").GetInt32();

        // Act — try to update as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var userBType = await CreateExpenseTypeAsync(userBClient, "UserBType");
        var userBTypeId = userBType.GetProperty("id").GetInt32();

        var response = await userBClient.PutAsJsonAsync($"/api/expenses/{createdId}", new
        {
            dueDate = "2024-06-15",
            description = "Hacked",
            expenseTypeId = userBTypeId,
            value = 9999m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenExpenseTypeIdBelongsToOtherUser()
    {
        // Arrange — create type as user B, expense as admin
        using var adminClient = await AuthenticateAsync();
        var adminType = await CreateExpenseTypeAsync(adminClient, "AdminOwnType");
        var adminTypeId = adminType.GetProperty("id").GetInt32();

        var created = await CreateExpenseAsync(adminClient, adminTypeId);
        var createdId = created.GetProperty("id").GetInt32();

        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var userBType = await CreateExpenseTypeAsync(userBClient, "UserBOwnType");
        var userBTypeId = userBType.GetProperty("id").GetInt32();

        // Act — admin tries to update their expense using user B's type
        var response = await adminClient.PutAsJsonAsync($"/api/expenses/{createdId}", new
        {
            dueDate = "2024-06-15",
            description = "Update with wrong type",
            expenseTypeId = userBTypeId,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenNotRecurringWithFrequency()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "BadRecur");
        var typeId = type.GetProperty("id").GetInt32();
        var created = await CreateExpenseAsync(client, typeId);
        var createdId = created.GetProperty("id").GetInt32();

        var updatePayload = new
        {
            dueDate = "2024-06-15",
            description = "Invalid combo",
            expenseTypeId = typeId,
            value = 100m,
            isRecurring = false,
            frequency = "monthly"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/expenses/{createdId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE /api/expenses/{id}

    [Fact]
    public async Task Delete_ShouldReturn204_WhenOwnExpense()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "DeleteTest");
        var typeId = type.GetProperty("id").GetInt32();
        var created = await CreateExpenseAsync(client, typeId);
        var createdId = created.GetProperty("id").GetInt32();

        // Act
        var response = await client.DeleteAsync($"/api/expenses/{createdId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await client.GetAsync($"/api/expenses/{createdId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenOtherUsersExpense()
    {
        // Arrange — create expense as admin
        using var adminClient = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(adminClient, "AdminDel");
        var typeId = type.GetProperty("id").GetInt32();
        var created = await CreateExpenseAsync(adminClient, typeId);
        var createdId = created.GetProperty("id").GetInt32();

        // Act — try to delete as another user
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var response = await userBClient.DeleteAsync($"/api/expenses/{createdId}");

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
        var getAll = await Client.GetAsync("/api/expenses");
        getAll.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // GET by id
        var getById = await Client.GetAsync("/api/expenses/1");
        getById.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // POST
        var post = await Client.PostAsJsonAsync("/api/expenses", new
        {
            dueDate = "2024-06-15",
            description = "Unauthorized",
            expenseTypeId = 1,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // PUT
        var put = await Client.PutAsJsonAsync("/api/expenses/1", new
        {
            dueDate = "2024-06-15",
            description = "Unauthorized",
            expenseTypeId = 1,
            value = 100m,
            isRecurring = false,
            frequency = (string?)null
        });
        put.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // DELETE
        var delete = await Client.DeleteAsync("/api/expenses/1");
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Multi-tenant isolation

    [Fact]
    public async Task Isolation_UserACannotReadUpdateOrDeleteUserBExpense()
    {
        // Arrange — create expense as user B
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var userBType = await CreateExpenseTypeAsync(userBClient, "UserBIso");
        var userBTypeId = userBType.GetProperty("id").GetInt32();

        var expenseB = await CreateExpenseAsync(userBClient, userBTypeId, new
        {
            dueDate = "2024-06-15",
            description = "User B expense",
            expenseTypeId = userBTypeId,
            value = 750m,
            isRecurring = false,
            frequency = (string?)null
        });
        var expenseBId = expenseB.GetProperty("id").GetInt32();

        // Act & Assert — admin (user A) cannot read user B's expense
        using var adminClient = await AuthenticateAsync();
        var adminType = await CreateExpenseTypeAsync(adminClient, "AdminIso");
        var adminTypeId = adminType.GetProperty("id").GetInt32();

        var getResponse = await adminClient.GetAsync($"/api/expenses/{expenseBId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Admin cannot update user B's expense
        var putResponse = await adminClient.PutAsJsonAsync($"/api/expenses/{expenseBId}", new
        {
            dueDate = "2024-06-15",
            description = "Hacked by admin",
            expenseTypeId = adminTypeId,
            value = 1m,
            isRecurring = false,
            frequency = (string?)null
        });
        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Admin cannot delete user B's expense
        var deleteResponse = await adminClient.DeleteAsync($"/api/expenses/{expenseBId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Referential integrity — ExpenseType with linked expenses

    [Fact]
    public async Task DeleteExpenseType_ShouldReturn409_WhenTypeHasLinkedExpenses()
    {
        // Arrange — create type and an expense using it
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "LinkedType");
        var typeId = type.GetProperty("id").GetInt32();

        await CreateExpenseAsync(client, typeId);

        // Act — try to delete the type
        var response = await client.DeleteAsync($"/api/expense-types/{typeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteExpenseType_ShouldReturn204_WhenTypeHasNoExpenses()
    {
        // Arrange — create type but no expenses
        using var client = await AuthenticateAsync();
        var type = await CreateExpenseTypeAsync(client, "UnlinkedType");
        var typeId = type.GetProperty("id").GetInt32();

        // Act — delete the type (no linked expenses)
        var response = await client.DeleteAsync($"/api/expense-types/{typeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion
}
