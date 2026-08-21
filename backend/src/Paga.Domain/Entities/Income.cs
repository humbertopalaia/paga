using Paga.Domain.Enums;

namespace Paga.Domain.Entities;

/// <summary>
/// A money inflow entry, optionally recurring.
/// </summary>
public class Income
{
    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; }
    public decimal Value { get; private set; }
    public bool IsRecurring { get; private set; }
    public RecurrenceFrequency? Frequency { get; private set; }

    public Income(Guid userId, DateOnly date, string description, decimal value, bool isRecurring, RecurrenceFrequency? frequency)
    {
        UserId = userId;
        Date = date;
        Description = description;
        Value = value;
        IsRecurring = isRecurring;
        Frequency = frequency;
    }

    /// <summary>
    /// Updates all mutable fields for controlled mutation.
    /// </summary>
    /// <param name="date">The new date for this income.</param>
    /// <param name="description">The new description.</param>
    /// <param name="value">The new monetary value.</param>
    /// <param name="isRecurring">Whether this income recurs.</param>
    /// <param name="frequency">The recurrence frequency (required when recurring, null otherwise).</param>
    public void Update(DateOnly date, string description, decimal value, bool isRecurring, RecurrenceFrequency? frequency)
    {
        Date = date;
        Description = description;
        Value = value;
        IsRecurring = isRecurring;
        Frequency = frequency;
    }

    // EF Core requires a parameterless constructor; kept private to enforce invariants.
    private Income()
    {
        Description = string.Empty;
    }
}
