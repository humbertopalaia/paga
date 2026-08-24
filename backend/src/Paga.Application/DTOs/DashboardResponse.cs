namespace Paga.Application.DTOs;

/// <summary>
/// Represents a single expense type with its aggregated total for the queried month.
/// </summary>
public record ExpenseByTypeItem(string TypeName, decimal Total);

/// <summary>
/// Aggregated financial metrics returned by the dashboard endpoint.
/// </summary>
public record DashboardResponse(
    decimal CurrentBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpense,
    IReadOnlyList<ExpenseByTypeItem> ExpensesByType);
