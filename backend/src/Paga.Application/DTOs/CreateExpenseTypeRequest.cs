namespace Paga.Application.DTOs;

/// <summary>
/// Payload for creating a new expense type.
/// </summary>
public record CreateExpenseTypeRequest
{
    /// <summary>Name of the expense type (max 100 characters, unique per user).</summary>
    public required string Name { get; init; }
}
