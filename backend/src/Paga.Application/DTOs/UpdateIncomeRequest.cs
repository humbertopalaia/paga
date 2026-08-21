namespace Paga.Application.DTOs;

/// <summary>
/// Payload for updating an existing income.
/// </summary>
public record UpdateIncomeRequest
{
    /// <summary>Date of the income (yyyy-MM-dd).</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Description of the income (max 300 characters).</summary>
    public required string Description { get; init; }

    /// <summary>Monetary value (must be greater than zero).</summary>
    public required decimal Value { get; init; }

    /// <summary>Whether this income recurs periodically.</summary>
    public required bool IsRecurring { get; init; }

    /// <summary>Recurrence frequency: weekly, monthly, or yearly. Required when IsRecurring is true; must be null otherwise.</summary>
    public string? Frequency { get; init; }
}
