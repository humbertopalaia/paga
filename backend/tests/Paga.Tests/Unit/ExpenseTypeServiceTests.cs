using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Paga.Application.Abstractions;
using Paga.Application.DTOs;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Services;

namespace Paga.Tests.Unit;

public class ExpenseTypeServiceTests
{
    private static readonly Guid CurrentUserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public ExpenseTypeServiceTests()
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

    private ExpenseTypeService CreateService(PagaDbContext context)
    {
        return new ExpenseTypeService(context, _currentUserMock.Object);
    }

    private async Task<ExpenseType> SeedExpenseTypeAsync(PagaDbContext context, Guid userId, string name = "Alimentação")
    {
        var entity = new ExpenseType(userId, name);
        context.ExpenseTypes.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyCurrentUserTypes()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        await SeedExpenseTypeAsync(context, CurrentUserId, "Lazer");
        await SeedExpenseTypeAsync(context, OtherUserId, "Educação");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseTypeFilter(null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Name).Should().BeEquivalentTo(["Lazer", "Transporte"]);
        result.TotalCount.Should().Be(2);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExpenseType_WhenOwnedByCurrentUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedExpenseTypeAsync(context, CurrentUserId, "Saúde");
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Saúde");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var act = () => service.GetByIdAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenOwnedByOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedExpenseTypeAsync(context, OtherUserId, "Educação");
        var service = CreateService(context);

        // Act
        var act = () => service.GetByIdAsync(entity.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ShouldCreateAndReturnDto_WhenNameIsUnique()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new CreateExpenseTypeRequest { Name = "Moradia" };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Moradia");
        result.Id.Should().BeGreaterThan(0);

        var persisted = await context.ExpenseTypes.FirstOrDefaultAsync(et => et.Id == result.Id);
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflictException_WhenDuplicateName()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        var service = CreateService(context);
        var request = new CreateExpenseTypeRequest { Name = "transporte" }; // case-insensitive

        // Act
        var act = () => service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*tipo de despesa*");
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ShouldUpdateName_WhenValid()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedExpenseTypeAsync(context, CurrentUserId, "Antigo");
        var service = CreateService(context);
        var request = new UpdateExpenseTypeRequest { Name = "Novo" };

        // Act
        var result = await service.UpdateAsync(entity.Id, request);

        // Assert
        result.Name.Should().Be("Novo");
        var persisted = await context.ExpenseTypes.FirstAsync(et => et.Id == entity.Id);
        persisted.Name.Should().Be("Novo");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new UpdateExpenseTypeRequest { Name = "Qualquer" };

        // Act
        var act = () => service.UpdateAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowConflictException_WhenDuplicateName()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedExpenseTypeAsync(context, CurrentUserId, "Lazer");
        var target = await SeedExpenseTypeAsync(context, CurrentUserId, "Saúde");
        var service = CreateService(context);
        var request = new UpdateExpenseTypeRequest { Name = "lazer" }; // case-insensitive

        // Act
        var act = () => service.UpdateAsync(target.Id, request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*tipo de despesa*");
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ShouldRemoveExpenseType_WhenNoLinkedExpenses()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        var service = CreateService(context);

        // Act
        await service.DeleteAsync(entity.Id);

        // Assert
        var exists = await context.ExpenseTypes.AnyAsync(et => et.Id == entity.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act
        var act = () => service.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowConflictException_WhenExpensesExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var expenseType = await SeedExpenseTypeAsync(context, CurrentUserId, "Alimentação");
        var expense = new Expense(CurrentUserId, DateOnly.FromDateTime(DateTime.Today), "Almoço", expenseType.Id, 25.00m, false, null);
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        // Act
        var act = () => service.DeleteAsync(expenseType.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*despesas vinculadas*");
    }
}
