using Microsoft.EntityFrameworkCore;
using Paga.Application.Abstractions;
using Paga.Application.Common;
using Paga.Application.DTOs;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence;

namespace Paga.Infrastructure.Services;

/// <summary>
/// Provides administrative CRUD operations for users.
/// </summary>
public class UserService : IUserService
{
    private readonly PagaDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(PagaDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task<PagedResult<UserResponse>> GetAllAsync(UserFilter filter, CancellationToken ct = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(u => u.Name.ToLower().Contains(filter.Name.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            query = query.Where(u => u.Email.ToLower().Contains(filter.Email.ToLower()));
        }

        var projected = query
            .OrderBy(u => u.Name)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.CreatedAt));

        return await projected.ToPagedResultAsync(filter.PageNumber, filter.PageSize, ct);
    }

    /// <inheritdoc />
    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.CreatedAt))
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        return user;
    }

    /// <inheritdoc />
    public async Task<UserResponse> CreateAsync(CreateUserRequest dto, CancellationToken ct = default)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower(), ct);

        if (emailExists)
        {
            throw new ConflictException("O email informado já está cadastrado.");
        }

        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var passwordHash = _passwordHasher.Hash(dto.Password);

        var user = new User(id, dto.Name, dto.Email, passwordHash, createdAt);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        return new UserResponse(id, dto.Name, dto.Email, createdAt);
    }

    /// <inheritdoc />
    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest dto, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        var emailDuplicate = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != id, ct);

        if (emailDuplicate)
        {
            throw new ConflictException("O email informado já está cadastrado.");
        }

        string? newPasswordHash = null;
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            newPasswordHash = _passwordHasher.Hash(dto.Password);
        }

        user.Update(dto.Name, dto.Email, newPasswordHash);
        await _context.SaveChangesAsync(ct);

        return new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            throw new NotFoundException("Usuário não encontrado.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(ct);
    }
}
