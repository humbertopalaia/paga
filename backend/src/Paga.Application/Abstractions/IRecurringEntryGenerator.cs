namespace Paga.Application.Abstractions;

/// <summary>
/// Generates recurring income/expense entries for the next upcoming period.
/// </summary>
public interface IRecurringEntryGenerator
{
    /// <summary>
    /// Processes all recurring entries for all users, generating next-period entries.
    /// </summary>
    Task GenerateAsync(DateOnly referenceDate, CancellationToken ct = default);
}
