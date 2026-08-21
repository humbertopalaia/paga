namespace Paga.Application.DTOs;

/// <summary>
/// Query parameters for filtering and paginating the expense list.
/// </summary>
public record ExpenseFilter(
    DateOnly? DueDateFrom,
    DateOnly? DueDateTo,
    int? ExpenseTypeId,
    string? Description,
    bool? IsRecurring,
    int PageNumber = 1,
    int PageSize = 10);
