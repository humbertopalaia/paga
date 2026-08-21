using Paga.Domain.Enums;

namespace Paga.Domain.Entities;

/// <summary>
/// A money outflow entry, optionally recurring, classified by an expense type.
/// </summary>
public class Expense
{
    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Description { get; private set; }
    public int ExpenseTypeId { get; private set; }
    public decimal Value { get; private set; }
    public bool IsRecurring { get; private set; }
    public RecurrenceFrequency? Frequency { get; private set; }

    public Expense(Guid userId, DateOnly dueDate, string description, int expenseTypeId, decimal value, bool isRecurring, RecurrenceFrequency? frequency)
    {
        UserId = userId;
        DueDate = dueDate;
        Description = description;
        ExpenseTypeId = expenseTypeId;
        Value = value;
        IsRecurring = isRecurring;
        Frequency = frequency;
    }

    /// <summary>
    /// Updates all mutable fields for controlled mutation.
    /// </summary>
    /// <param name="dueDate">The new due date for this expense.</param>
    /// <param name="description">The new description.</param>
    /// <param name="expenseTypeId">The new expense type identifier.</param>
    /// <param name="value">The new monetary value.</param>
    /// <param name="isRecurring">Whether this expense recurs.</param>
    /// <param name="frequency">The recurrence frequency (required when recurring, null otherwise).</param>
    public void Update(DateOnly dueDate, string description, int expenseTypeId, decimal value, bool isRecurring, RecurrenceFrequency? frequency)
    {
        DueDate = dueDate;
        Description = description;
        ExpenseTypeId = expenseTypeId;
        Value = value;
        IsRecurring = isRecurring;
        Frequency = frequency;
    }

    // EF Core requires a parameterless constructor; kept private to enforce invariants.
    private Expense()
    {
        Description = string.Empty;
    }
}
