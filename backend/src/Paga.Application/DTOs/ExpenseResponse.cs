namespace Paga.Application.DTOs;

/// <summary>
/// Public representation of an expense returned by the API.
/// </summary>
public record ExpenseResponse(
    int Id,
    string DueDate,
    string Description,
    int ExpenseTypeId,
    string ExpenseTypeName,
    decimal Value,
    bool IsRecurring,
    string? Frequency);
