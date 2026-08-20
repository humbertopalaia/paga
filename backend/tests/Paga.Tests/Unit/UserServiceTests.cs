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

public class UserServiceTests
{
    private readonly Mock<IPasswordHasher> _passwordHasherMock;

    public UserServiceTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _passwordHasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed-password");
    }

    private PagaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PagaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PagaDbContext(options);
    }

    private UserService CreateService(PagaDbContext context)
    {
        return new UserService(context, _passwordHasherMock.Object);
    }

    private async Task<User> SeedUserAsync(PagaDbContext context, string email = "existing@test.com", string passwordHash = "original-hash")
    {
        var user = new User(Guid.NewGuid(), "Existing User", email, passwordHash, DateTime.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflictException_WhenEmailDuplicate()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedUserAsync(context, "duplicate@test.com");
        var service = CreateService(context);

        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "duplicate@test.com",
            Password = "password123"
        };

        // Act
        var act = () => service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*email*cadastrado*");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnUserResponse_WhenDataValid()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "new@test.com",
            Password = "password123"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New User");
        result.Email.Should().Be("new@test.com");
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _passwordHasherMock.Verify(x => x.Hash("password123"), Times.Once);

        var persisted = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
        persisted.Should().NotBeNull();
        persisted!.PasswordHash.Should().Be("hashed-password");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateHash_WhenPasswordProvided()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = await SeedUserAsync(context, "update@test.com", "original-hash");
        var service = CreateService(context);

        _passwordHasherMock.Setup(x => x.Hash("new-password")).Returns("new-hash");

        var request = new UpdateUserRequest
        {
            Name = "Updated User",
            Email = "update@test.com",
            Password = "new-password"
        };

        // Act
        await service.UpdateAsync(user.Id, request);

        // Assert
        var updatedUser = await context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.PasswordHash.Should().Be("new-hash");
        updatedUser.Name.Should().Be("Updated User");
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreserveHash_WhenPasswordNotProvided()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = await SeedUserAsync(context, "preserve@test.com", "original-hash");
        var service = CreateService(context);

        var request = new UpdateUserRequest
        {
            Name = "Updated Name",
            Email = "preserve@test.com",
            Password = null
        };

        // Act
        await service.UpdateAsync(user.Id, request);

        // Assert
        var updatedUser = await context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.PasswordHash.Should().Be("original-hash");
        updatedUser.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowConflictException_WhenEmailBelongsToAnother()
    {
        // Arrange
        using var context = CreateDbContext();
        await SeedUserAsync(context, "other@test.com");
        var targetUser = await SeedUserAsync(context, "target@test.com");
        var service = CreateService(context);

        var request = new UpdateUserRequest
        {
            Name = "Target User",
            Email = "other@test.com",
            Password = null
        };

        // Act
        var act = () => service.UpdateAsync(targetUser.Id, request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*email*cadastrado*");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser_WhenUserExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = await SeedUserAsync(context, "delete@test.com");
        var service = CreateService(context);

        // Act
        await service.DeleteAsync(user.Id);

        // Assert
        var exists = await context.Users.AnyAsync(u => u.Id == user.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => service.DeleteAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
