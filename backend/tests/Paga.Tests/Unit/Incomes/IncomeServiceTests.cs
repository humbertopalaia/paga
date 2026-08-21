using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Paga.Application.Abstractions;
using Paga.Application.DTOs;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Domain.Enums;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Services;

namespace Paga.Tests.Unit.Incomes;

public class IncomeServiceTests
{
    private static readonly Guid CurrentUserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public IncomeServiceTests()
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

    private IncomeService CreateService(PagaDbContext context)
    {
        return new IncomeService(context, _currentUserMock.Object);
    }

    private async Task<Income> SeedIncomeAsync(
        PagaDbContext context,
        Guid userId,
        DateOnly? date = null,
        string description = "Salário",
        decimal value = 5000m,
        bool isRecurring = false,
        RecurrenceFrequency? frequency = null)
    {
        var entity = new Income(
            userId,
            date ?? new DateOnly(2024, 6, 15),
            description,
            value,
            isRecurring,
            frequency);
        context.Incomes.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyCurrentUserIncomes()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedIncomeAsync(context, CurrentUserId, description: "Salário");
        await SeedIncomeAsync(context, CurrentUserId, description: "Freelance");
        await SeedIncomeAsync(context, OtherUserId, description: "Aluguel recebido");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new IncomeFilter(null, null, null, null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Salário", "Freelance"]);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateFrom()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 1, 10), description: "Janeiro");
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 3, 20), description: "Março");
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 6, 1), description: "Junho");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new IncomeFilter(new DateOnly(2024, 3, 1), null, null, null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Março", "Junho"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateTo()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 1, 10), description: "Janeiro");
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 3, 20), description: "Março");
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 6, 1), description: "Junho");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new IncomeFilter(null, new DateOnly(2024, 3, 31), null, null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Janeiro", "Março"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDescription_CaseInsensitive()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedIncomeAsync(context, CurrentUserId, description: "Salário mensal");
        await SeedIncomeAsync(context, CurrentUserId, description: "Freelance design");
        await SeedIncomeAsync(context, CurrentUserId, description: "Consultoria SALÁRIO extra");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new IncomeFilter(null, null, "salário", null));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Salário mensal", "Consultoria SALÁRIO extra"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByIsRecurring()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedIncomeAsync(context, CurrentUserId, description: "Salário", isRecurring: true, frequency: RecurrenceFrequency.Monthly);
        await SeedIncomeAsync(context, CurrentUserId, description: "Bônus", isRecurring: false);
        await SeedIncomeAsync(context, CurrentUserId, description: "Aluguel", isRecurring: true, frequency: RecurrenceFrequency.Monthly);
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new IncomeFilter(null, null, null, true));

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Description).Should().BeEquivalentTo(["Salário", "Aluguel"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderByDateDescending()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 1, 1), description: "Primeiro");
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 6, 15), description: "Meio");
        await SeedIncomeAsync(context, CurrentUserId, date: new DateOnly(2024, 12, 31), description: "Último");
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync(new IncomeFilter(null, null, null, null));

        // Assert
        result.Items.Select(i => i.Description).Should().ContainInOrder("Último", "Meio", "Primeiro");
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ShouldReturnIncome_WhenOwnedByCurrentUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedIncomeAsync(context, CurrentUserId, description: "Freelance", value: 3000m);
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        result.Description.Should().Be("Freelance");
        result.Value.Should().Be(3000m);
        result.Date.Should().Be("2024-06-15");
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
        var entity = await SeedIncomeAsync(context, OtherUserId, description: "Renda do outro");
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
        var service = CreateService(context);
        var request = new CreateIncomeRequest
        {
            Date = new DateOnly(2024, 7, 1),
            Description = "Consultoria",
            Value = 4500m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Date.Should().Be("2024-07-01");
        result.Description.Should().Be("Consultoria");
        result.Value.Should().Be(4500m);
        result.IsRecurring.Should().BeFalse();
        result.Frequency.Should().BeNull();

        var persisted = await context.Incomes.FirstOrDefaultAsync(i => i.Id == result.Id);
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateRecurringIncome_WithFrequency()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new CreateIncomeRequest
        {
            Date = new DateOnly(2024, 7, 1),
            Description = "Salário",
            Value = 8000m,
            IsRecurring = true,
            Frequency = "monthly"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("monthly");

        var persisted = await context.Incomes.FirstOrDefaultAsync(i => i.Id == result.Id);
        persisted.Should().NotBeNull();
        persisted!.Frequency.Should().Be(RecurrenceFrequency.Monthly);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAllFields_WhenValid()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedIncomeAsync(context, CurrentUserId, description: "Original", value: 1000m);
        var service = CreateService(context);
        var request = new UpdateIncomeRequest
        {
            Date = new DateOnly(2024, 8, 20),
            Description = "Atualizado",
            Value = 2500m,
            IsRecurring = false,
            Frequency = null
        };

        // Act
        var result = await service.UpdateAsync(entity.Id, request);

        // Assert
        result.Date.Should().Be("2024-08-20");
        result.Description.Should().Be("Atualizado");
        result.Value.Should().Be(2500m);
        result.IsRecurring.Should().BeFalse();
        result.Frequency.Should().BeNull();

        var persisted = await context.Incomes.FirstAsync(i => i.Id == entity.Id);
        persisted.Description.Should().Be("Atualizado");
        persisted.Value.Should().Be(2500m);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new UpdateIncomeRequest
        {
            Date = new DateOnly(2024, 8, 1),
            Description = "Qualquer",
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
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenOwnedByOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedIncomeAsync(context, OtherUserId, description: "Outro user");
        var service = CreateService(context);
        var request = new UpdateIncomeRequest
        {
            Date = new DateOnly(2024, 8, 1),
            Description = "Tentativa",
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
        var entity = await SeedIncomeAsync(context, CurrentUserId, description: "Freelance", isRecurring: false);
        var service = CreateService(context);
        var request = new UpdateIncomeRequest
        {
            Date = entity.Date,
            Description = entity.Description,
            Value = entity.Value,
            IsRecurring = true,
            Frequency = "weekly"
        };

        // Act
        var result = await service.UpdateAsync(entity.Id, request);

        // Assert
        result.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("weekly");

        var persisted = await context.Incomes.FirstAsync(i => i.Id == entity.Id);
        persisted.IsRecurring.Should().BeTrue();
        persisted.Frequency.Should().Be(RecurrenceFrequency.Weekly);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ShouldRemoveIncome_WhenOwnedByCurrentUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedIncomeAsync(context, CurrentUserId, description: "Para excluir");
        var service = CreateService(context);

        // Act
        await service.DeleteAsync(entity.Id);

        // Assert
        var exists = await context.Incomes.AnyAsync(i => i.Id == entity.Id);
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
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenOwnedByOtherUser()
    {
        // Arrange
        using var context = CreateDbContext();
        var entity = await SeedIncomeAsync(context, OtherUserId, description: "Do outro");
        var service = CreateService(context);

        // Act
        var act = () => service.DeleteAsync(entity.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
