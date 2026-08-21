namespace Paga.Application.DTOs;

/// <summary>
/// Public representation of an income returned by the API.
/// </summary>
public record IncomeResponse(
    int Id,
    string Date,
    string Description,
    decimal Value,
    bool IsRecurring,
    string? Frequency);
