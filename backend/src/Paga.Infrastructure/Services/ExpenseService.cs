using FluentValidation;
using FluentValidation.Results;
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

        var projected = query
            .Join(
                _context.ExpenseTypes,
                e => e.ExpenseTypeId,
                et => et.Id,
                (e, et) => new { e, et })
            .OrderByDescending(x => x.e.DueDate)
            .Select(x => new ExpenseResponse(
                x.e.Id,
                x.e.DueDate.ToString("yyyy-MM-dd"),
                x.e.Description,
                x.e.ExpenseTypeId,
                x.et.Name,
                x.e.Value,
                x.e.IsRecurring,
                x.e.Frequency != null ? x.e.Frequency.ToString()!.ToLower() : null));

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
                _context.ExpenseTypes,
                e => e.ExpenseTypeId,
                et => et.Id,
                (e, et) => new { e, et })
            .Select(x => new ExpenseResponse(
                x.e.Id,
                x.e.DueDate.ToString("yyyy-MM-dd"),
                x.e.Description,
                x.e.ExpenseTypeId,
                x.et.Name,
                x.e.Value,
                x.e.IsRecurring,
                x.e.Frequency != null ? x.e.Frequency.ToString()!.ToLower() : null))
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

        await ValidateExpenseTypeOwnershipAsync(dto.ExpenseTypeId, userId, ct);

        var frequency = ParseFrequency(dto.Frequency);

        var entity = new Expense(userId, dto.DueDate, dto.Description, dto.ExpenseTypeId, dto.Value, dto.IsRecurring, frequency);

        _context.Expenses.Add(entity);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
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

        await ValidateExpenseTypeOwnershipAsync(dto.ExpenseTypeId, userId, ct);

        var frequency = ParseFrequency(dto.Frequency);
        entity.Update(dto.DueDate, dto.Description, dto.ExpenseTypeId, dto.Value, dto.IsRecurring, frequency);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
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

        _context.Expenses.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Validates that the specified expense type exists and belongs to the authenticated user.
    /// Throws a <see cref="ValidationException"/> if the type is invalid or belongs to another user.
    /// </summary>
    private async Task ValidateExpenseTypeOwnershipAsync(int expenseTypeId, Guid userId, CancellationToken ct)
    {
        var typeExists = await _context.ExpenseTypes
            .AnyAsync(et => et.Id == expenseTypeId && et.UserId == userId, ct);

        if (!typeExists)
        {
            var failure = new ValidationFailure("ExpenseTypeId", "O tipo de despesa informado não existe ou não pertence ao usuário.");
            throw new ValidationException(new[] { failure });
        }
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
