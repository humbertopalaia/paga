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
/// Provides CRUD operations for incomes scoped to the authenticated user.
/// </summary>
public class IncomeService : IIncomeService
{
    private readonly PagaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public IncomeService(PagaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<PagedResult<IncomeResponse>> GetAllAsync(IncomeFilter filter, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var query = _context.Incomes.AsNoTracking()
            .Where(i => i.UserId == userId);

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(i => i.Date >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(i => i.Date <= filter.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Description))
        {
            query = query.Where(i => i.Description.ToLower().Contains(filter.Description.ToLower()));
        }

        if (filter.IsRecurring.HasValue)
        {
            query = query.Where(i => i.IsRecurring == filter.IsRecurring.Value);
        }

        var projected = query
            .OrderByDescending(i => i.Date)
            .Select(i => new IncomeResponse(
                i.Id,
                i.Date.ToString("yyyy-MM-dd"),
                i.Description,
                i.Value,
                i.IsRecurring,
                i.Frequency != null ? i.Frequency.ToString()!.ToLower() : null));

        return await projected.ToPagedResultAsync(filter.PageNumber, filter.PageSize, ct);
    }

    /// <inheritdoc />
    public async Task<IncomeResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var income = await _context.Incomes
            .AsNoTracking()
            .Where(i => i.Id == id && i.UserId == userId)
            .Select(i => new IncomeResponse(
                i.Id,
                i.Date.ToString("yyyy-MM-dd"),
                i.Description,
                i.Value,
                i.IsRecurring,
                i.Frequency != null ? i.Frequency.ToString()!.ToLower() : null))
            .FirstOrDefaultAsync(ct);

        if (income is null)
        {
            throw new NotFoundException("Receita não encontrada.");
        }

        return income;
    }

    /// <inheritdoc />
    public async Task<IncomeResponse> CreateAsync(CreateIncomeRequest dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var frequency = ParseFrequency(dto.Frequency);

        var entity = new Income(userId, dto.Date, dto.Description, dto.Value, dto.IsRecurring, frequency);

        _context.Incomes.Add(entity);
        await _context.SaveChangesAsync(ct);

        return new IncomeResponse(
            entity.Id,
            entity.Date.ToString("yyyy-MM-dd"),
            entity.Description,
            entity.Value,
            entity.IsRecurring,
            entity.Frequency?.ToString().ToLower());
    }

    /// <inheritdoc />
    public async Task<IncomeResponse> UpdateAsync(int id, UpdateIncomeRequest dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var entity = await _context.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);

        if (entity is null)
        {
            throw new NotFoundException("Receita não encontrada.");
        }

        var frequency = ParseFrequency(dto.Frequency);
        entity.Update(dto.Date, dto.Description, dto.Value, dto.IsRecurring, frequency);
        await _context.SaveChangesAsync(ct);

        return new IncomeResponse(
            entity.Id,
            entity.Date.ToString("yyyy-MM-dd"),
            entity.Description,
            entity.Value,
            entity.IsRecurring,
            entity.Frequency?.ToString().ToLower());
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var entity = await _context.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);

        if (entity is null)
        {
            throw new NotFoundException("Receita não encontrada.");
        }

        _context.Incomes.Remove(entity);
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
