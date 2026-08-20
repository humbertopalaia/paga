using Microsoft.EntityFrameworkCore;
using Paga.Application.Abstractions;
using Paga.Application.Common;
using Paga.Application.DTOs;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence;

namespace Paga.Infrastructure.Services;

/// <summary>
/// Provides CRUD operations for expense types scoped to the authenticated user.
/// </summary>
public class ExpenseTypeService : IExpenseTypeService
{
    private readonly PagaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ExpenseTypeService(PagaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ExpenseTypeResponse>> GetAllAsync(ExpenseTypeFilter filter, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var query = _context.ExpenseTypes.AsNoTracking()
            .Where(et => et.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(et => et.Name.ToLower().Contains(filter.Name.ToLower()));
        }

        var projected = query
            .OrderBy(et => et.Name)
            .Select(et => new ExpenseTypeResponse(et.Id, et.Name));

        return await projected.ToPagedResultAsync(filter.PageNumber, filter.PageSize, ct);
    }

    /// <inheritdoc />
    public async Task<ExpenseTypeResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var expenseType = await _context.ExpenseTypes
            .AsNoTracking()
            .Where(et => et.Id == id && et.UserId == userId)
            .Select(et => new ExpenseTypeResponse(et.Id, et.Name))
            .FirstOrDefaultAsync(ct);

        if (expenseType is null)
        {
            throw new NotFoundException("Tipo de despesa não encontrado.");
        }

        return expenseType;
    }

    /// <inheritdoc />
    public async Task<ExpenseTypeResponse> CreateAsync(CreateExpenseTypeRequest dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var nameExists = await _context.ExpenseTypes
            .AnyAsync(et => et.UserId == userId && et.Name.ToLower() == dto.Name.ToLower(), ct);

        if (nameExists)
        {
            throw new ConflictException("Já existe um tipo de despesa com este nome.");
        }

        var entity = new ExpenseType(userId, dto.Name);

        _context.ExpenseTypes.Add(entity);
        await _context.SaveChangesAsync(ct);

        return new ExpenseTypeResponse(entity.Id, entity.Name);
    }

    /// <inheritdoc />
    public async Task<ExpenseTypeResponse> UpdateAsync(int id, UpdateExpenseTypeRequest dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var entity = await _context.ExpenseTypes
            .FirstOrDefaultAsync(et => et.Id == id && et.UserId == userId, ct);

        if (entity is null)
        {
            throw new NotFoundException("Tipo de despesa não encontrado.");
        }

        var nameExists = await _context.ExpenseTypes
            .AnyAsync(et => et.UserId == userId && et.Name.ToLower() == dto.Name.ToLower() && et.Id != id, ct);

        if (nameExists)
        {
            throw new ConflictException("Já existe um tipo de despesa com este nome.");
        }

        entity.UpdateName(dto.Name);
        await _context.SaveChangesAsync(ct);

        return new ExpenseTypeResponse(entity.Id, entity.Name);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var entity = await _context.ExpenseTypes
            .FirstOrDefaultAsync(et => et.Id == id && et.UserId == userId, ct);

        if (entity is null)
        {
            throw new NotFoundException("Tipo de despesa não encontrado.");
        }

        var hasExpenses = await _context.Expenses
            .AnyAsync(e => e.ExpenseTypeId == id, ct);

        if (hasExpenses)
        {
            throw new ConflictException("Não é possível excluir um tipo de despesa que possui despesas vinculadas.");
        }

        _context.ExpenseTypes.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }
}
