using Microsoft.EntityFrameworkCore;
using Paga.Application.Abstractions;
using Paga.Application.DTOs;
using Paga.Infrastructure.Persistence;

namespace Paga.Infrastructure.Services;

/// <summary>
/// Computes aggregated financial metrics for the authenticated user's dashboard.
/// All queries are projected directly to DTOs — no entities are materialized in memory.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly PagaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(PagaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<DashboardResponse> GetAsync(int year, int month, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        // Query 1: Current balance (all-time)
        var totalIncome = await _context.Incomes
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .SumAsync(i => (decimal?)i.Value, ct) ?? 0m;

        var totalExpense = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .SumAsync(e => (decimal?)e.Value, ct) ?? 0m;

        var currentBalance = totalIncome - totalExpense;

        // Query 2: Monthly income
        var monthlyIncome = await _context.Incomes
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Date >= firstDay && i.Date <= lastDay)
            .SumAsync(i => (decimal?)i.Value, ct) ?? 0m;

        // Query 3: Expenses by type (monthly + grouped) using Join since Expense has no navigation property
        var expensesByType = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.DueDate >= firstDay && e.DueDate <= lastDay)
            .Join(
                _context.ExpenseTypes,
                e => e.ExpenseTypeId,
                et => et.Id,
                (e, et) => new { e.Value, TypeName = et.Name })
            .GroupBy(x => x.TypeName)
            .Select(g => new ExpenseByTypeItem(g.Key, g.Sum(x => x.Value)))
            .ToListAsync(ct);

        var monthlyExpense = expensesByType.Sum(x => x.Total);

        return new DashboardResponse(currentBalance, monthlyIncome, monthlyExpense, expensesByType);
    }
}
