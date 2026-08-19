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

    // EF Core requires a parameterless constructor; kept private to enforce invariants.
    private Expense()
    {
        Description = string.Empty;
    }
}
