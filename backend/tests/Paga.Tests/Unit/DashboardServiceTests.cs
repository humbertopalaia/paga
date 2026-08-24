using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Paga.Application.Abstractions;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Services;

namespace Paga.Tests.Unit;

public class DashboardServiceTests
{
    private static readonly Guid CurrentUserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public DashboardServiceTests()
    {
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(x => x.UserId).Returns(CurrentUserId);
    }

    private PagaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PagaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PagaDbContext(options);
    }

    private DashboardService CreateService(PagaDbContext context)
    {
        return new DashboardService(context, _currentUserMock.Object);
    }

    // --- Helpers ---

    private static Income CreateIncome(Guid userId, DateOnly date, decimal value)
    {
        return new Income(userId, date, "Income", value, false, null);
    }

    private static ExpenseType CreateExpenseType(Guid userId, string name)
    {
        return new ExpenseType(userId, name);
    }

    private static Expense CreateExpense(Guid userId, DateOnly dueDate, int expenseTypeId, decimal value)
    {
        return new Expense(userId, dueDate, "Expense", expenseTypeId, value, false, null);
    }

    // --- GetAsync: No data ---

    [Fact]
    public async Task GetAsync_ShouldReturnZerosAndEmptyArray_WhenNoData()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetAsync(2024, 6);

        // Assert
        result.CurrentBalance.Should().Be(0m);
        result.MonthlyIncome.Should().Be(0m);
        result.MonthlyExpense.Should().Be(0m);
        result.ExpensesByType.Should().BeEmpty();
    }

    // --- GetAsync: Incomes and expenses in month ---

    [Fact]
    public async Task GetAsync_ShouldReturnCorrectSums_WhenIncomesAndExpensesExistInMonth()
    {
        // Arrange
        using var context = CreateDbContext();

        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 3, 5), 1000m));
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 3, 20), 2000m));

        var expenseType = CreateExpenseType(CurrentUserId, "Alimentação");
        context.ExpenseTypes.Add(expenseType);
        await context.SaveChangesAsync();

        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 3, 10), expenseType.Id, 500m));
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 3, 25), expenseType.Id, 300m));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAsync(2024, 3);

        // Assert
        result.MonthlyIncome.Should().Be(3000m);
        result.MonthlyExpense.Should().Be(800m);
        result.CurrentBalance.Should().Be(2200m);
    }

    // --- GetAsync: Specific month different from current ---

    [Fact]
    public async Task GetAsync_ShouldFilterBySpecificMonth_WhenMonthIsNotCurrent()
    {
        // Arrange
        using var context = CreateDbContext();

        // January income
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 1, 15), 5000m));
        // March income
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 3, 10), 7000m));

        var expenseType = CreateExpenseType(CurrentUserId, "Transporte");
        context.ExpenseTypes.Add(expenseType);
        await context.SaveChangesAsync();

        // January expense
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 1, 20), expenseType.Id, 1000m));
        // March expense
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 3, 5), expenseType.Id, 2000m));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act — query January specifically
        var result = await service.GetAsync(2024, 1);

        // Assert
        result.MonthlyIncome.Should().Be(5000m);
        result.MonthlyExpense.Should().Be(1000m);
        // currentBalance is all-time: (5000 + 7000) - (1000 + 2000) = 9000
        result.CurrentBalance.Should().Be(9000m);
    }

    // --- GetAsync: Multiple expense types ---

    [Fact]
    public async Task GetAsync_ShouldGroupByExpenseType_WhenMultipleTypesExist()
    {
        // Arrange
        using var context = CreateDbContext();

        var type1 = CreateExpenseType(CurrentUserId, "Alimentação");
        var type2 = CreateExpenseType(CurrentUserId, "Transporte");
        var type3 = CreateExpenseType(CurrentUserId, "Lazer");
        context.ExpenseTypes.AddRange(type1, type2, type3);
        await context.SaveChangesAsync();

        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 6, 1), type1.Id, 400m));
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 6, 15), type1.Id, 600m));
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 6, 10), type2.Id, 200m));
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 6, 20), type3.Id, 150m));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAsync(2024, 6);

        // Assert
        result.ExpensesByType.Should().HaveCount(3);
        result.ExpensesByType.Should().Contain(x => x.TypeName == "Alimentação" && x.Total == 1000m);
        result.ExpensesByType.Should().Contain(x => x.TypeName == "Transporte" && x.Total == 200m);
        result.ExpensesByType.Should().Contain(x => x.TypeName == "Lazer" && x.Total == 150m);
        result.MonthlyExpense.Should().Be(1350m);
    }

    // --- GetAsync: Negative balance ---

    [Fact]
    public async Task GetAsync_ShouldReturnNegativeBalance_WhenExpensesExceedIncomes()
    {
        // Arrange
        using var context = CreateDbContext();

        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 5, 1), 1000m));

        var expenseType = CreateExpenseType(CurrentUserId, "Moradia");
        context.ExpenseTypes.Add(expenseType);
        await context.SaveChangesAsync();

        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 5, 10), expenseType.Id, 3000m));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAsync(2024, 5);

        // Assert
        result.CurrentBalance.Should().Be(-2000m);
    }

    // --- GetAsync: Different months contribute only to queried month ---

    [Fact]
    public async Task GetAsync_ShouldOnlyIncludeQueriedMonth_WhenIncomesSpanMultipleMonths()
    {
        // Arrange
        using var context = CreateDbContext();

        // February
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 2, 10), 2000m));
        // March (queried month)
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 3, 1), 3000m));
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 3, 31), 1500m));
        // April
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 4, 5), 4000m));

        var expenseType = CreateExpenseType(CurrentUserId, "Geral");
        context.ExpenseTypes.Add(expenseType);
        await context.SaveChangesAsync();

        // February expense
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 2, 15), expenseType.Id, 500m));
        // March expenses (queried month)
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 3, 10), expenseType.Id, 700m));
        // April expense
        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 4, 1), expenseType.Id, 900m));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAsync(2024, 3);

        // Assert
        result.MonthlyIncome.Should().Be(4500m); // 3000 + 1500
        result.MonthlyExpense.Should().Be(700m);
        // currentBalance all-time: (2000+3000+1500+4000) - (500+700+900) = 10500 - 2100 = 8400
        result.CurrentBalance.Should().Be(8400m);
    }

    // --- GetAsync: User isolation ---

    [Fact]
    public async Task GetAsync_ShouldExcludeOtherUserData_WhenMultipleUsersExist()
    {
        // Arrange
        using var context = CreateDbContext();

        // Current user data
        context.Incomes.Add(CreateIncome(CurrentUserId, new DateOnly(2024, 7, 5), 5000m));

        var currentUserType = CreateExpenseType(CurrentUserId, "Alimentação");
        context.ExpenseTypes.Add(currentUserType);
        await context.SaveChangesAsync();

        context.Expenses.Add(CreateExpense(CurrentUserId, new DateOnly(2024, 7, 10), currentUserType.Id, 1000m));

        // Other user data (should not affect results)
        context.Incomes.Add(CreateIncome(OtherUserId, new DateOnly(2024, 7, 1), 99000m));

        var otherUserType = CreateExpenseType(OtherUserId, "Educação");
        context.ExpenseTypes.Add(otherUserType);
        await context.SaveChangesAsync();

        context.Expenses.Add(CreateExpense(OtherUserId, new DateOnly(2024, 7, 15), otherUserType.Id, 50000m));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAsync(2024, 7);

        // Assert
        result.CurrentBalance.Should().Be(4000m); // 5000 - 1000 (only current user)
        result.MonthlyIncome.Should().Be(5000m);
        result.MonthlyExpense.Should().Be(1000m);
        result.ExpensesByType.Should().HaveCount(1);
        result.ExpensesByType.First().TypeName.Should().Be("Alimentação");
        result.ExpensesByType.First().Total.Should().Be(1000m);
    }
}
