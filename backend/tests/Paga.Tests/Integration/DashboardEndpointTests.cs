using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Paga.Application.DTOs;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Integration tests for the GET /api/dashboard endpoint.
/// Validates correct response shape, month filtering, validation, authentication, and multi-tenant isolation.
/// </summary>
[Collection("Integration")]
public class DashboardEndpointTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DashboardEndpointTests(PostgresFixture fixture) : base(fixture)
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
    /// Creates an expense type via API and returns its id.
    /// </summary>
    private async Task<int> CreateExpenseTypeAsync(HttpClient client, string name)
    {
        var uniqueName = $"{name}_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/expense-types", new { name = uniqueName });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Creates an income via API.
    /// </summary>
    private async Task CreateIncomeAsync(HttpClient client, string date, decimal value)
    {
        var payload = new
        {
            date,
            description = $"Income_{Guid.NewGuid():N}",
            value,
            isRecurring = false,
            frequency = (string?)null
        };
        var response = await client.PostAsJsonAsync("/api/incomes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates an expense via API.
    /// </summary>
    private async Task CreateExpenseAsync(HttpClient client, string dueDate, decimal value, int expenseTypeId)
    {
        var payload = new
        {
            dueDate,
            description = $"Expense_{Guid.NewGuid():N}",
            expenseTypeId,
            value,
            isRecurring = false,
            frequency = (string?)null
        };
        var response = await client.PostAsJsonAsync("/api/expenses", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region GET /api/dashboard → 200 with correct shape

    [Fact]
    public async Task GetDashboard_ShouldReturn200_WithCorrectShape()
    {
        // Arrange
        using var client = await AuthenticateAsync();

        // Act
        var response = await client.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("currentBalance", out _).Should().BeTrue();
        json.TryGetProperty("monthlyIncome", out _).Should().BeTrue();
        json.TryGetProperty("monthlyExpense", out _).Should().BeTrue();
        json.TryGetProperty("expensesByType", out var expensesByType).Should().BeTrue();
        expensesByType.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturn200_WithCorrectCalculations()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var typeId = await CreateExpenseTypeAsync(client, "Alimentação");

        // Create incomes in March 2024
        await CreateIncomeAsync(client, "2024-03-10", 5000.00m);
        await CreateIncomeAsync(client, "2024-03-20", 3000.00m);

        // Create expenses in March 2024
        await CreateExpenseAsync(client, "2024-03-05", 1200.00m, typeId);
        await CreateExpenseAsync(client, "2024-03-15", 800.00m, typeId);

        // Act
        var response = await client.GetAsync("/api/dashboard?month=2024-03");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.MonthlyIncome.Should().Be(8000.00m);
        result.MonthlyExpense.Should().Be(2000.00m);
        result.ExpensesByType.Should().NotBeEmpty();
        result.ExpensesByType.Sum(x => x.Total).Should().Be(result.MonthlyExpense);
    }

    #endregion

    #region GET /api/dashboard?month=2024-03 → filtered data

    [Fact]
    public async Task GetDashboard_ShouldReturnFilteredData_WhenMonthSpecified()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var typeId = await CreateExpenseTypeAsync(client, "Transporte");

        // Create income in February 2024
        await CreateIncomeAsync(client, "2024-02-15", 4000.00m);

        // Create income in March 2024
        await CreateIncomeAsync(client, "2024-03-15", 6000.00m);

        // Create expense in February 2024
        await CreateExpenseAsync(client, "2024-02-10", 500.00m, typeId);

        // Create expense in March 2024
        await CreateExpenseAsync(client, "2024-03-10", 1500.00m, typeId);

        // Act — query March 2024
        var response = await client.GetAsync("/api/dashboard?month=2024-03");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        result.Should().NotBeNull();

        // monthlyIncome should only include March income
        result!.MonthlyIncome.Should().Be(6000.00m);

        // monthlyExpense should only include March expenses
        result.MonthlyExpense.Should().Be(1500.00m);

        // currentBalance is all-time: (4000+6000) - (500+1500) = 8000
        // Note: other tests may have seeded data for this user, so we verify the March-specific
        // monthly values and that balance >= the all-time sum from these entries
        result.CurrentBalance.Should().BeGreaterThanOrEqualTo(8000.00m);

        // expensesByType should reflect March only
        result.ExpensesByType.Should().HaveCountGreaterThanOrEqualTo(1);
        result.ExpensesByType.Sum(x => x.Total).Should().Be(result.MonthlyExpense);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnExpensesByType_GroupedCorrectly()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var type1Id = await CreateExpenseTypeAsync(client, "Lazer");
        var type2Id = await CreateExpenseTypeAsync(client, "Saúde");

        // Create expenses in April 2024 with different types
        await CreateExpenseAsync(client, "2024-04-05", 300.00m, type1Id);
        await CreateExpenseAsync(client, "2024-04-10", 200.00m, type1Id);
        await CreateExpenseAsync(client, "2024-04-15", 700.00m, type2Id);

        // Act
        var response = await client.GetAsync("/api/dashboard?month=2024-04");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.MonthlyExpense.Should().Be(1200.00m);
        result.ExpensesByType.Should().HaveCount(2);

        // Verify grouping totals
        var lazerGroup = result.ExpensesByType.FirstOrDefault(x => x.Total == 500.00m);
        lazerGroup.Should().NotBeNull();

        var saudeGroup = result.ExpensesByType.FirstOrDefault(x => x.Total == 700.00m);
        saudeGroup.Should().NotBeNull();
    }

    #endregion

    #region GET /api/dashboard?month=invalid → 400

    [Theory]
    [InlineData("invalid")]
    [InlineData("2024-13")]
    [InlineData("2024-00")]
    [InlineData("2024-1")]
    [InlineData("202403")]
    [InlineData("abcd-ef")]
    public async Task GetDashboard_ShouldReturn400_WhenMonthInvalid(string invalidMonth)
    {
        // Arrange
        using var client = await AuthenticateAsync();

        // Act
        var response = await client.GetAsync($"/api/dashboard?month={invalidMonth}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetInt32().Should().Be(400);
    }

    #endregion

    #region No token → 401

    [Fact]
    public async Task GetDashboard_ShouldReturn401_WithoutToken()
    {
        // Act — use unauthenticated client from base class
        var response = await Client.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region User isolation

    [Fact]
    public async Task GetDashboard_ShouldIsolateUserData_UserADoesNotSeeUserBData()
    {
        // Arrange — create user B with data in May 2024
        using var userBClient = await CreateAndAuthenticateSecondUserAsync();
        var userBTypeId = await CreateExpenseTypeAsync(userBClient, "UserB_Tipo");

        await CreateIncomeAsync(userBClient, "2024-05-10", 10000.00m);
        await CreateExpenseAsync(userBClient, "2024-05-10", 3000.00m, userBTypeId);

        // Create user A (admin) with NO data in May 2024
        using var adminClient = await AuthenticateAsync();

        // Act — admin queries May 2024
        var response = await adminClient.GetAsync("/api/dashboard?month=2024-05");

        // Assert — admin should see zero for May monthly values (no own data in May)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        result.Should().NotBeNull();

        // Admin has no income/expense in May 2024 specifically
        result!.MonthlyIncome.Should().Be(0m);
        result.MonthlyExpense.Should().Be(0m);
        result.ExpensesByType.Should().BeEmpty();

        // Verify user B sees their own data
        var userBResponse = await userBClient.GetAsync("/api/dashboard?month=2024-05");
        userBResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userBResult = await userBResponse.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        userBResult.Should().NotBeNull();
        userBResult!.MonthlyIncome.Should().Be(10000.00m);
        userBResult.MonthlyExpense.Should().Be(3000.00m);
        userBResult.ExpensesByType.Should().NotBeEmpty();
    }

    #endregion

    #region Month with no data → zeros and empty array

    [Fact]
    public async Task GetDashboard_ShouldReturnZeros_WhenMonthHasNoData()
    {
        // Arrange — use a month far in the future where no data exists
        using var client = await AuthenticateAsync();

        // Act — query a distant future month
        var response = await client.GetAsync("/api/dashboard?month=2099-12");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.MonthlyIncome.Should().Be(0m);
        result.MonthlyExpense.Should().Be(0m);
        result.ExpensesByType.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnAllTimeBalance_RegardlessOfMonth()
    {
        // Arrange — create a fresh user with known data
        using var userClient = await CreateAndAuthenticateSecondUserAsync();
        var typeId = await CreateExpenseTypeAsync(userClient, "BalanceTest");

        // Income in Jan 2024 = 10000
        await CreateIncomeAsync(userClient, "2024-01-15", 10000.00m);
        // Expense in Jan 2024 = 3000
        await CreateExpenseAsync(userClient, "2024-01-15", 3000.00m, typeId);

        // Act — query a different month (June 2024)
        var response = await userClient.GetAsync("/api/dashboard?month=2024-06");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        result.Should().NotBeNull();

        // currentBalance is all-time: 10000 - 3000 = 7000
        result!.CurrentBalance.Should().Be(7000.00m);

        // Monthly values for June should be zero (no data in June)
        result.MonthlyIncome.Should().Be(0m);
        result.MonthlyExpense.Should().Be(0m);
        result.ExpensesByType.Should().BeEmpty();
    }

    #endregion
}
