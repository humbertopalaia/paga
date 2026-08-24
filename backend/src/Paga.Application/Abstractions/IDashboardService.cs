using Paga.Application.DTOs;

namespace Paga.Application.Abstractions;

/// <summary>
/// Computes aggregated financial metrics for the authenticated user's dashboard.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Returns dashboard metrics for the specified month (or current month if null).
    /// </summary>
    /// <param name="year">Year component of the month to query.</param>
    /// <param name="month">Month component (1-12) to query.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<DashboardResponse> GetAsync(int year, int month, CancellationToken ct = default);
}
