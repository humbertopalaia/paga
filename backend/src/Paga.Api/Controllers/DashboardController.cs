using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paga.Application.Abstractions;
using Paga.Application.DTOs;

namespace Paga.Api.Controllers;

/// <summary>
/// Provides aggregated financial metrics for the authenticated user's dashboard.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public partial class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Returns financial metrics (balance, monthly income/expense, expenses by type) for the authenticated user.
    /// </summary>
    /// <param name="month">Optional month in YYYY-MM format. Defaults to the current month when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] string? month, CancellationToken ct)
    {
        var (year, monthNum) = ParseMonth(month);
        if (year == 0)
        {
            return Problem(
                detail: "O parâmetro 'month' deve estar no formato YYYY-MM com mês entre 01 e 12.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed");
        }

        var result = await _dashboardService.GetAsync(year, monthNum, ct);
        return Ok(result);
    }

    private static (int Year, int Month) ParseMonth(string? month)
    {
        if (string.IsNullOrWhiteSpace(month))
        {
            var now = DateTime.UtcNow;
            return (now.Year, now.Month);
        }

        if (!MonthRegex().IsMatch(month))
        {
            return (0, 0);
        }

        var parts = month.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial Regex MonthRegex();
}
