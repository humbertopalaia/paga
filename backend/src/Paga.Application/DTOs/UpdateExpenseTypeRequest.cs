namespace Paga.Application.DTOs;

/// <summary>
/// Payload for updating an existing expense type.
/// </summary>
public record UpdateExpenseTypeRequest
{
    /// <summary>New name for the expense type (max 100 characters, unique per user).</summary>
    public required string Name { get; init; }
}
