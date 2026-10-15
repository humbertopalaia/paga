using Paga.Domain.Enums;

namespace Paga.Domain.Services;

/// <summary>
/// Pure date computation for recurrence scheduling.
/// </summary>
public static class DateCalculator
{
    /// <summary>
    /// Computes the next occurrence date given a source date and frequency.
    /// For weekly: source date + 7 days.
    /// For monthly: same day-of-month in the next calendar month (clamped to last day if needed).
    /// For yearly: same month/day in the next calendar year (Feb 29 → Feb 28 in non-leap years).
    /// </summary>
    /// <param name="sourceDate">The date from which to compute the next occurrence.</param>
    /// <param name="frequency">The recurrence frequency (weekly, monthly, or yearly).</param>
    /// <returns>The next occurrence date.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when frequency is not a valid value.</exception>
    public static DateOnly GetNextOccurrence(DateOnly sourceDate, RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Weekly => sourceDate.AddDays(7),
            RecurrenceFrequency.Monthly => GetNextMonthlyOccurrence(sourceDate),
            RecurrenceFrequency.Yearly => GetNextYearlyOccurrence(sourceDate),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Invalid recurrence frequency.")
        };
    }

    /// <summary>
    /// Determines whether a target date falls within the generation horizon
    /// (at most 1 period ahead of the reference date).
    /// Weekly: target &lt;= reference + 7 days.
    /// Monthly: target &lt;= last day of the next calendar month from reference.
    /// Yearly: target &lt;= same day next year from reference (clamped).
    /// </summary>
    /// <param name="targetDate">The date to evaluate.</param>
    /// <param name="referenceDate">The current reference date (typically today).</param>
    /// <param name="frequency">The recurrence frequency.</param>
    /// <returns>True if the target date is within the generation horizon; otherwise false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when frequency is not a valid value.</exception>
    public static bool IsWithinHorizon(DateOnly targetDate, DateOnly referenceDate, RecurrenceFrequency frequency)
    {
        if (targetDate <= referenceDate)
            return false;

        var horizon = GetHorizonDate(referenceDate, frequency);
        return targetDate <= horizon;
    }

    private static DateOnly GetNextMonthlyOccurrence(DateOnly sourceDate)
    {
        var nextMonth = sourceDate.Month == 12 ? 1 : sourceDate.Month + 1;
        var nextYear = sourceDate.Month == 12 ? sourceDate.Year + 1 : sourceDate.Year;
        var daysInMonth = DateTime.DaysInMonth(nextYear, nextMonth);
        var day = Math.Min(sourceDate.Day, daysInMonth);

        return new DateOnly(nextYear, nextMonth, day);
    }

    private static DateOnly GetNextYearlyOccurrence(DateOnly sourceDate)
    {
        var nextYear = sourceDate.Year + 1;
        var daysInMonth = DateTime.DaysInMonth(nextYear, sourceDate.Month);
        var day = Math.Min(sourceDate.Day, daysInMonth);

        return new DateOnly(nextYear, sourceDate.Month, day);
    }

    private static DateOnly GetHorizonDate(DateOnly referenceDate, RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Weekly => referenceDate.AddDays(7),
            RecurrenceFrequency.Monthly => GetMonthlyHorizon(referenceDate),
            RecurrenceFrequency.Yearly => GetYearlyHorizon(referenceDate),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Invalid recurrence frequency.")
        };
    }

    private static DateOnly GetMonthlyHorizon(DateOnly referenceDate)
    {
        // Horizon = last day of the next calendar month from reference
        var nextMonth = referenceDate.Month == 12 ? 1 : referenceDate.Month + 1;
        var nextYear = referenceDate.Month == 12 ? referenceDate.Year + 1 : referenceDate.Year;
        var daysInMonth = DateTime.DaysInMonth(nextYear, nextMonth);

        return new DateOnly(nextYear, nextMonth, daysInMonth);
    }

    private static DateOnly GetYearlyHorizon(DateOnly referenceDate)
    {
        // Horizon = same day next year from reference (clamped)
        var nextYear = referenceDate.Year + 1;
        var daysInMonth = DateTime.DaysInMonth(nextYear, referenceDate.Month);
        var day = Math.Min(referenceDate.Day, daysInMonth);

        return new DateOnly(nextYear, referenceDate.Month, day);
    }
}
