namespace Paga.Application.DTOs;

/// <summary>
/// Payload for updating an existing expense.
/// </summary>
public record UpdateExpenseRequest
{
    /// <summary>Due date of the expense (yyyy-MM-dd).</summary>
    public required DateOnly DueDate { get; init; }

    /// <summary>Description of the expense (max 300 characters).</summary>
    public required string Description { get; init; }

    /// <summary>Expense type identifier.</summary>
    public required int ExpenseTypeId { get; init; }

    /// <summary>Monetary value (must be greater than zero).</summary>
    public required decimal Value { get; init; }

    /// <summary>Whether this expense recurs periodically.</summary>
    public required bool IsRecurring { get; init; }

    /// <summary>Recurrence frequency: weekly, monthly, or yearly. Required when IsRecurring is true; must be null otherwise.</summary>
    public string? Frequency { get; init; }
}
