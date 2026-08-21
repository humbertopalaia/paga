using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Application.Abstractions;

/// <summary>
/// Provides CRUD operations for incomes scoped to the authenticated user.
/// </summary>
public interface IIncomeService
{
    /// <summary>
    /// Lists incomes with optional filters (date range, description, recurrence) and pagination.
    /// </summary>
    Task<PagedResult<IncomeResponse>> GetAllAsync(IncomeFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Gets a single income by identifier (scoped to current user).
    /// </summary>
    Task<IncomeResponse> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new income for the authenticated user.
    /// </summary>
    Task<IncomeResponse> CreateAsync(CreateIncomeRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing income's fields.
    /// </summary>
    Task<IncomeResponse> UpdateAsync(int id, UpdateIncomeRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes an income owned by the authenticated user.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
