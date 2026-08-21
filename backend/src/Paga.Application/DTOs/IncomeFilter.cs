namespace Paga.Application.DTOs;

/// <summary>
/// Query parameters for filtering and paginating the income list.
/// </summary>
public record IncomeFilter(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Description,
    bool? IsRecurring,
    int PageNumber = 1,
    int PageSize = 10);
