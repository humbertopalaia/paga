using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Paga.Application.Abstractions;
using Paga.Domain.Entities;
using Paga.Domain.Enums;
using Paga.Domain.Services;
using Paga.Infrastructure.Persistence;

namespace Paga.Infrastructure.Services;

/// <summary>
/// Generates recurring income/expense entries for the next upcoming period.
/// Processes each user independently with separate units-of-work for fault isolation.
/// </summary>
public class RecurringEntryGenerator : IRecurringEntryGenerator
{
    private readonly PagaDbContext _context;
    private readonly ILogger<RecurringEntryGenerator> _logger;

    public RecurringEntryGenerator(PagaDbContext context, ILogger<RecurringEntryGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task GenerateAsync(DateOnly referenceDate, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting recurring entry generation for reference date {ReferenceDate}", referenceDate);

        var userIds = await GetUserIdsWithRecurringEntriesAsync(ct);

        _logger.LogInformation("Found {UserCount} users with recurring entries", userIds.Count);

        foreach (var userId in userIds)
        {
            try
            {
                await ProcessUserAsync(userId, referenceDate, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process recurring entries for user {UserId}. Skipping.", userId);
            }
        }

        _logger.LogInformation("Recurring entry generation completed for reference date {ReferenceDate}", referenceDate);
    }

    /// <summary>
    /// Retrieves distinct user IDs that have at least one recurring income or expense.
    /// </summary>
    private async Task<List<Guid>> GetUserIdsWithRecurringEntriesAsync(CancellationToken ct)
    {
        var incomeUserIds = _context.Incomes
            .AsNoTracking()
            .Where(i => i.IsRecurring && i.Frequency != null)
            .Select(i => i.UserId);

        var expenseUserIds = _context.Expenses
            .AsNoTracking()
            .Where(e => e.IsRecurring && e.Frequency != null)
            .Select(e => e.UserId);

        return await incomeUserIds
            .Union(expenseUserIds)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Processes all recurring entries for a single user, generating missing entries within the horizon.
    /// Each user's work is a separate unit-of-work (SaveChanges per user).
    /// </summary>
    private async Task ProcessUserAsync(Guid userId, DateOnly referenceDate, CancellationToken ct)
    {
        var incomesGenerated = await GenerateIncomesForUserAsync(userId, referenceDate, ct);
        var expensesGenerated = await GenerateExpensesForUserAsync(userId, referenceDate, ct);

        if (incomesGenerated + expensesGenerated > 0)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Generated {IncomeCount} income(s) and {ExpenseCount} expense(s) for user {UserId}",
                incomesGenerated, expensesGenerated, userId);
        }
    }

    /// <summary>
    /// Generates recurring income entries for a user. Returns the count of entries created.
    /// </summary>
    private async Task<int> GenerateIncomesForUserAsync(Guid userId, DateOnly referenceDate, CancellationToken ct)
    {
        var recurringIncomes = await _context.Incomes
            .Where(i => i.UserId == userId && i.IsRecurring && i.Frequency != null)
            .ToListAsync(ct);

        var count = 0;

        foreach (var source in recurringIncomes)
        {
            var baseDate = source.LastGeneratedDate ?? source.Date;
            var nextDate = DateCalculator.GetNextOccurrence(baseDate, source.Frequency!.Value);

            if (!DateCalculator.IsWithinHorizon(nextDate, referenceDate, source.Frequency!.Value))
            {
                continue;
            }

            // Check if a generated entry already exists for this source + date
            var alreadyExists = await _context.Incomes
                .AnyAsync(i => i.SourceEntryId == source.Id && i.Date == nextDate, ct);

            if (alreadyExists)
            {
                continue;
            }

            var generated = Income.CreateGenerated(source, nextDate);
            _context.Incomes.Add(generated);
            source.SetLastGeneratedDate(nextDate);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Generates recurring expense entries for a user. Returns the count of entries created.
    /// </summary>
    private async Task<int> GenerateExpensesForUserAsync(Guid userId, DateOnly referenceDate, CancellationToken ct)
    {
        var recurringExpenses = await _context.Expenses
            .Where(e => e.UserId == userId && e.IsRecurring && e.Frequency != null)
            .ToListAsync(ct);

        var count = 0;

        foreach (var source in recurringExpenses)
        {
            var baseDate = source.LastGeneratedDate ?? source.DueDate;
            var nextDate = DateCalculator.GetNextOccurrence(baseDate, source.Frequency!.Value);

            if (!DateCalculator.IsWithinHorizon(nextDate, referenceDate, source.Frequency!.Value))
            {
                continue;
            }

            // Check if a generated entry already exists for this source + due date
            var alreadyExists = await _context.Expenses
                .AnyAsync(e => e.SourceEntryId == source.Id && e.DueDate == nextDate, ct);

            if (alreadyExists)
            {
                continue;
            }

            var generated = Expense.CreateGenerated(source, nextDate);
            _context.Expenses.Add(generated);
            source.SetLastGeneratedDate(nextDate);
            count++;
        }

        return count;
    }
}
