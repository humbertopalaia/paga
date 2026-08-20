namespace Paga.Application.DTOs;

/// <summary>
/// Query parameters for filtering and paginating the user list.
/// </summary>
/// <param name="Name">Optional case-insensitive name filter (contains match).</param>
/// <param name="Email">Optional case-insensitive email filter (contains match).</param>
/// <param name="PageNumber">Page number (1-based, defaults to 1).</param>
/// <param name="PageSize">Page size (defaults to 10, maximum 100).</param>
public record UserFilter(string? Name, string? Email, int PageNumber = 1, int PageSize = 10);
