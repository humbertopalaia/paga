using Microsoft.EntityFrameworkCore;
using Paga.Application.Abstractions;
using Paga.Application.Common;
using Paga.Application.DTOs;
using Paga.Application.Exceptions;
using Paga.Domain.Entities;
using Paga.Domain.Enums;
using Paga.Infrastructure.Persistence;

namespace Paga.Infrastructure.Services;

/// <summary>
/// Provides CRUD operations for expenses scoped to the authenticated user.
/// </summary>
public class ExpenseService : IExpenseService
{
    private readonly PagaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ExpenseService(PagaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ExpenseResponse>> GetAllAsync(ExpenseFilter filter, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var query = _context.Expenses.AsNoTracking()
            .Where(e => e.UserId == userId);

        if (filter.DueDateFrom.HasValue)
        {
            query = query.Where(e => e.DueDate >= filter.DueDateFrom.Value);
        }

        if (filter.DueDateTo.HasValue)
        {
            query = query.Where(e => e.DueDate <= filter.DueDateTo.Value);
        }

        if (filter.ExpenseTypeId.HasValue)
        {
            query = query.Where(e => e.ExpenseTypeId == filter.ExpenseTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Description))
        {
            query = query.Where(e => e.Description.ToLower().Contains(filter.Description.ToLower()));
        }

        if (filter.IsRecurring.HasValue)
        {
            query = query.Where(e => e.IsRecurring == filter.IsRecurring.Value);
        }

        if (filter.IsGenerated.HasValue)
        {
            query = query.Where(e => e.IsGenerated == filter.IsGenerated.Value);
        }

        var projected = query
            .Join(
                _context.ExpenseTypes.AsNoTracking(),
                e => e.ExpenseTypeId,
                et => et.Id,
                (e, et) => new ExpenseResponse(
                    e.Id,
                    e.DueDate.ToString("yyyy-MM-dd"),
                    e.Description,
                    e.ExpenseTypeId,
                    et.Name,
                    e.Value,
                    e.IsRecurring,
                    e.Frequency != null ? e.Frequency.ToString()!.ToLower() : null,
                    e.IsGenerated,
                    e.SourceEntryId))
            .OrderByDescending(e => e.DueDate);

        return await projected.ToPagedResultAsync(filter.PageNumber, filter.PageSize, ct);
    }

    /// <inheritdoc />
    public async Task<ExpenseResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var expense = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.Id == id && e.UserId == userId)
            .Join(
                _context.ExpenseTypes.AsNoTracking(),
                e => e.ExpenseTypeId,
                et => et.Id,
                (e, et) => new ExpenseResponse(
                    e.Id,
                    e.DueDate.ToString("yyyy-MM-dd"),
                    e.Description,
                    e.ExpenseTypeId,
                    et.Name,
                    e.Value,
                    e.IsRecurring,
                    e.Frequency != null ? e.Frequency.ToString()!.ToLower() : null,
                    e.IsGenerated,
                    e.SourceEntryId))
            .FirstOrDefaultAsync(ct);

        if (expense is null)
        {
            throw new NotFoundException("Despesa não encontrada.");
        }

        return expense;
    }

    /// <inheritdoc />
    public async Task<ExpenseResponse> CreateAsync(CreateExpenseRequest dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var frequency = ParseFrequency(dto.Frequency);

        // Validate that the expense type belongs to the authenticated user
        var expenseTypeExists = await _context.ExpenseTypes
            .AnyAsync(et => et.Id == dto.ExpenseTypeId && et.UserId == userId, ct);

        if (!expenseTypeExists)
        {
            throw new NotFoundException("Tipo de despesa não encontrado.");
        }

        var entity = new Expense(userId, dto.DueDate, dto.Description, dto.ExpenseTypeId, dto.Value, dto.IsRecurring, frequency);

        _context.Expenses.Add(entity);
        await _context.SaveChangesAsync(ct);

        var typeName = await _context.ExpenseTypes
            .Where(et => et.Id == entity.ExpenseTypeId)
            .Select(et => et.Name)
            .FirstAsync(ct);

        return new ExpenseResponse(
            entity.Id,
            entity.DueDate.ToString("yyyy-MM-dd"),
            entity.Description,
            entity.ExpenseTypeId,
            typeName,
            entity.Value,
            entity.IsRecurring,
            entity.Frequency?.ToString().ToLower(),
            entity.IsGenerated,
            entity.SourceEntryId);
    }

    /// <inheritdoc />
    public async Task<ExpenseResponse> UpdateAsync(int id, UpdateExpenseRequest dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var entity = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

        if (entity is null)
        {
            throw new NotFoundException("Despesa não encontrada.");
        }

        // Validate that the expense type belongs to the authenticated user
        var expenseTypeExists = await _context.ExpenseTypes
            .AnyAsync(et => et.Id == dto.ExpenseTypeId && et.UserId == userId, ct);

        if (!expenseTypeExists)
        {
            throw new NotFoundException("Tipo de despesa não encontrado.");
        }

        var frequency = ParseFrequency(dto.Frequency);
        entity.Update(dto.DueDate, dto.Description, dto.ExpenseTypeId, dto.Value, dto.IsRecurring, frequency);
        await _context.SaveChangesAsync(ct);

        var typeName = await _context.ExpenseTypes
            .Where(et => et.Id == entity.ExpenseTypeId)
            .Select(et => et.Name)
            .FirstAsync(ct);

        return new ExpenseResponse(
            entity.Id,
            entity.DueDate.ToString("yyyy-MM-dd"),
            entity.Description,
            entity.ExpenseTypeId,
            typeName,
            entity.Value,
            entity.IsRecurring,
            entity.Frequency?.ToString().ToLower(),
            entity.IsGenerated,
            entity.SourceEntryId);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var entity = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

        if (entity is null)
        {
            throw new NotFoundException("Despesa não encontrada.");
        }

        // Orphan generated children before deleting the source (Requirement 1.10)
        var generatedChildren = await _context.Expenses
            .Where(e => e.SourceEntryId == entity.Id)
            .ToListAsync(ct);

        foreach (var child in generatedChildren)
        {
            child.ClearSourceEntry();
        }

        _context.Expenses.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Parses a frequency string (weekly/monthly/yearly) to the domain enum.
    /// Returns null when the input is null or empty.
    /// </summary>
    private static RecurrenceFrequency? ParseFrequency(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
        {
            return null;
        }

        if (Enum.TryParse<RecurrenceFrequency>(frequency, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
