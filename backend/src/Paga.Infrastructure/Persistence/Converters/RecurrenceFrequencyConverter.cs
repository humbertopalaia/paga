using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Paga.Domain.Enums;

namespace Paga.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts <see cref="RecurrenceFrequency"/> to its lowercase text representation for persistence.
/// Maps: Weekly → "weekly", Monthly → "monthly", Yearly → "yearly", null → null.
/// </summary>
public class RecurrenceFrequencyConverter : ValueConverter<RecurrenceFrequency?, string?>
{
    public RecurrenceFrequencyConverter() : base(
        v => v == null ? null : ToText(v.Value),
        v => v == null ? null : FromText(v))
    {
    }

    private static string ToText(RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Weekly => "weekly",
        RecurrenceFrequency.Monthly => "monthly",
        RecurrenceFrequency.Yearly => "yearly",
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unknown recurrence frequency.")
    };

    private static RecurrenceFrequency FromText(string text) => text switch
    {
        "weekly" => RecurrenceFrequency.Weekly,
        "monthly" => RecurrenceFrequency.Monthly,
        "yearly" => RecurrenceFrequency.Yearly,
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, "Unknown recurrence frequency text.")
    };
}
