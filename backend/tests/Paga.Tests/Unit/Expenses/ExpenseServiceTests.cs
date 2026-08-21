using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Paga.Application.Abstractions;
using Paga.Application.DTOs;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Domain.Enums;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Services;

namespace Paga.Tests.Unit.Expenses;

public class ExpenseServiceTests
{
    private static readonly Guid CurrentUserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public ExpenseServiceTests()
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

    private ExpenseService CreateService(PagaDbContext context)
    {
        return new ExpenseService(context, _currentUserMock.Object);
    }

    private async Task<ExpenseType> SeedExpenseTypeAsync(
        PagaDbContext context,
        Guid userId,
        string name = "Alimentação")
    {
        var entity = new ExpenseType(userId, name);
        context.ExpenseTypes.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    private async Task<Expense> SeedExpenseAsync(
        PagaDbContext context,
        Guid userId,
        int expenseTypeId,
        DateOnly? dueDate = null,
        string description = "Supermercado",
        decimal value = 250m,
        bool isRecurring = false,
        RecurrenceFrequency? frequency = null)
    {
        var entity = new Expense(
            userId,
            dueDate ?? new DateOnly(2024, 6, 15),
            description,
            expenseTypeId,
            value,
            isRecurring,
            frequency);
        context.Expenses.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyCurrentUserExpenses()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        var otherType = await SeedExpenseTypeAsync(context, OtherUserId, "Lazer");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Uber");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Metrô");
        await SeedExpenseAsync(context, OtherUserId, otherType.Id, description: "Cinema");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(null, null, null, null, null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Uber", "Metrô"]);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDueDateFrom()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId);
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 1, 10), description: "Janeiro");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 3, 20), description: "Março");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 6, 1), description: "Junho");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(new DateOnly(2024, 3, 1), null, null, null, null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Março", "Junho"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDueDateTo()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId);
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 1, 10), description: "Janeiro");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 3, 20), description: "Março");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 6, 1), description: "Junho");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(null, new DateOnly(2024, 3, 31), null, null, null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Janeiro", "Março"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByExpenseTypeId()
    {
        // Arrange
        using var context = CreateDbContext();
        var typeA = await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        var typeB = await SeedExpenseTypeAsync(context, CurrentUserId, "Alimentação");
        await SeedExpenseAsync(context, CurrentUserId, typeA.Id, description: "Uber");
        await SeedExpenseAsync(context, CurrentUserId, typeB.Id, description: "Restaurante");
        await SeedExpenseAsync(context, CurrentUserId, typeA.Id, description: "Gasolina");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(null, null, typeA.Id, null, null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Uber", "Gasolina"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDescription_CaseInsensitive()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId);
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Supermercado mensal");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Freelance design");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "SUPERMERCADO extra");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(null, null, null, "supermercado", null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Supermercado mensal", "SUPERMERCADO extra"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByIsRecurring()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId);
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Aluguel", isRecurring: true, frequency: RecurrenceFrequency.Monthly);
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Jantar", isRecurring: false);
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Internet", isRecurring: true, frequency: RecurrenceFrequency.Monthly);
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(null, null, null, null, true));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Aluguel", "Internet"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderByDueDateDescending()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId);
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 1, 1), description: "Primeiro");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 6, 15), description: "Meio");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, dueDate: new DateOnly(2024, 12, 31), description: "Último");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(null, null, null, null, null));

        // Assert
        result.Items.Select(i => i.Description).Should().ContainInOrder("Último", "Meio", "Primeiro");
    }

    [Fact]
    public async Task GetAllAsync_ShouldIncludeExpenseTypeName()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Uber");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new ExpenseFilter(null, null, null, null, null));

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().ExpenseTypeName.Should().Be("Transporte");
        result.Items.First().ExpenseTypeId.Should().Be(type.Id);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExpense_WhenExistsForCurrentUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId, "Alimentação");
        var entity = await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Restaurante", value: 80m);
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        result.Description.Should().Be("Restaurante");
        result.Value.Should().Be(80m);
        result.DueDate.Should().Be("2024-06-15");
        result.ExpenseTypeName.Should().Be("Alimentação");
        result.ExpenseTypeId.Should().Be(type.Id);
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
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var otherType = await SeedExpenseTypeAsync(context, OtherUserId, "Lazer");
        var entity = await SeedExpenseAsync(context, OtherUserId, otherType.Id, description: "Cinema");
        var service = CreateService(context);

        // Act
        var act = () => service.GetByIdAsync(entity.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ShouldCreateAndReturnDto_WhenValid()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        var service = CreateService(context);
        var request = new CreateExpenseRequest
        {
            DueDate = new DateOnly(2024, 7, 1),
            Description = "Uber",
            ExpenseTypeId = type.Id,
            Value = 45.50m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.DueDate.Should().Be("2024-07-01");
        result.Description.Should().Be("Uber");
        result.ExpenseTypeId.Should().Be(type.Id);
        result.ExpenseTypeName.Should().Be("Transporte");
        result.Value.Should().Be(45.50m);
        result.IsRecurring.Should().BeFalse();
        result.Frequency.Should().BeNull();

        var persisted = await context.Expenses.FirstOrDefaultAsync(e => e.Id == result.Id);
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateRecurringExpense_WhenFrequencyProvided()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId, "Moradia");
        var service = CreateService(context);
        var request = new CreateExpenseRequest
        {
            DueDate = new DateOnly(2024, 7, 1),
            Description = "Aluguel",
            ExpenseTypeId = type.Id,
            Value = 2500m,
            IsRecurring = true,
            Frequency = "monthly"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("monthly");

        var persisted = await context.Expenses.FirstOrDefaultAsync(e => e.Id == result.Id);
        persisted.Should().NotBeNull();
        persisted!.Frequency.Should().Be(RecurrenceFrequency.Monthly);
    }

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenExpenseTypeIdBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var otherType = await SeedExpenseTypeAsync(context, OtherUserId, "Tipo do Outro");
        var service = CreateService(context);
        var request = new CreateExpenseRequest
        {
            DueDate = new DateOnly(2024, 7, 1),
            Description = "Tentativa",
            ExpenseTypeId = otherType.Id,
            Value = 100m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var act = () => service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*tipo de despesa*");
    }

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenExpenseTypeIdDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new CreateExpenseRequest
        {
            DueDate = new DateOnly(2024, 7, 1),
            Description = "Tentativa",
            ExpenseTypeId = 9999,
            Value = 100m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var act = () => service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*tipo de despesa*");
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAllFields_WhenValid()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId, "Alimentação");
        var entity = await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Original", value: 100m);
        var service = CreateService(context);
        var request = new UpdateExpenseRequest
        {
            DueDate = new DateOnly(2024, 8, 20),
            Description = "Atualizado",
            ExpenseTypeId = type.Id,
            Value = 250m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var result = await service.UpdateAsync(entity.Id, request);

        // Assert
        result.DueDate.Should().Be("2024-08-20");
        result.Description.Should().Be("Atualizado");
        result.Value.Should().Be(250m);
        result.IsRecurring.Should().BeFalse();
        result.Frequency.Should().BeNull();
        result.ExpenseTypeName.Should().Be("Alimentação");

        var persisted = await context.Expenses.FirstAsync(e => e.Id == entity.Id);
        persisted.Description.Should().Be("Atualizado");
        persisted.Value.Should().Be(250m);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId);
        var service = CreateService(context);
        var request = new UpdateExpenseRequest
        {
            DueDate = new DateOnly(2024, 8, 1),
            Description = "Qualquer",
            ExpenseTypeId = type.Id,
            Value = 100m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var act = () => service.UpdateAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var otherType = await SeedExpenseTypeAsync(context, OtherUserId, "Tipo do outro");
        var myType = await SeedExpenseTypeAsync(context, CurrentUserId, "Meu tipo");
        var entity = await SeedExpenseAsync(context, OtherUserId, otherType.Id, description: "Gasto do outro");
        var service = CreateService(context);
        var request = new UpdateExpenseRequest
        {
            DueDate = new DateOnly(2024, 8, 1),
            Description = "Tentativa",
            ExpenseTypeId = myType.Id,
            Value = 100m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var act = () => service.UpdateAsync(entity.Id, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldToggleRecurrence_FromFalseToTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId, "Serviços");
        var entity = await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Internet", isRecurring: false);
        var service = CreateService(context);
        var request = new UpdateExpenseRequest
        {
            DueDate = entity.DueDate,
            Description = entity.Description,
            ExpenseTypeId = type.Id,
            Value = entity.Value,
            IsRecurring = true,
            Frequency = "weekly"
        };

        // Act
        var result = await service.UpdateAsync(entity.Id, request);

        // Assert
        result.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("weekly");

        var persisted = await context.Expenses.FirstAsync(e => e.Id == entity.Id);
        persisted.IsRecurring.Should().BeTrue();
        persisted.Frequency.Should().Be(RecurrenceFrequency.Weekly);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeExpenseTypeId()
    {
        // Arrange
        using var context = CreateDbContext();
        var typeA = await SeedExpenseTypeAsync(context, CurrentUserId, "Transporte");
        var typeB = await SeedExpenseTypeAsync(context, CurrentUserId, "Alimentação");
        var entity = await SeedExpenseAsync(context, CurrentUserId, typeA.Id, description: "Gasto");
        var service = CreateService(context);
        var request = new UpdateExpenseRequest
        {
            DueDate = entity.DueDate,
            Description = entity.Description,
            ExpenseTypeId = typeB.Id,
            Value = entity.Value,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var result = await service.UpdateAsync(entity.Id, request);

        // Assert
        result.ExpenseTypeId.Should().Be(typeB.Id);
        result.ExpenseTypeName.Should().Be("Alimentação");

        var persisted = await context.Expenses.FirstAsync(e => e.Id == entity.Id);
        persisted.ExpenseTypeId.Should().Be(typeB.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReject_WhenNewExpenseTypeIdBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var myType = await SeedExpenseTypeAsync(context, CurrentUserId, "Meu tipo");
        var otherType = await SeedExpenseTypeAsync(context, OtherUserId, "Tipo do outro");
        var entity = await SeedExpenseAsync(context, CurrentUserId, myType.Id, description: "Minha despesa");
        var service = CreateService(context);
        var request = new UpdateExpenseRequest
        {
            DueDate = entity.DueDate,
            Description = entity.Description,
            ExpenseTypeId = otherType.Id,
            Value = entity.Value,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var act = () => service.UpdateAsync(entity.Id, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*tipo de despesa*");
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ShouldDelete_WhenExistsForCurrentUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var type = await SeedExpenseTypeAsync(context, CurrentUserId);
        var entity = await SeedExpenseAsync(context, CurrentUserId, type.Id, description: "Para excluir");
        var service = CreateService(context);

        // Act
        await service.DeleteAsync(entity.Id);

        // Assert
        var exists = await context.Expenses.AnyAsync(e => e.Id == entity.Id);
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
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenBelongsToOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var otherType = await SeedExpenseTypeAsync(context, OtherUserId, "Tipo outro");
        var entity = await SeedExpenseAsync(context, OtherUserId, otherType.Id, description: "Do outro");
        var service = CreateService(context);

        // Act
        var act = () => service.DeleteAsync(entity.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
