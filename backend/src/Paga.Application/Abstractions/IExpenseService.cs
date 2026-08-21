using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Application.Abstractions;

/// <summary>
/// Provides CRUD operations for expenses scoped to the authenticated user.
/// </summary>
public interface IExpenseService
{
    /// <summary>
    /// Lists expenses with optional filters (due date range, expense type, description, recurrence) and pagination.
    /// </summary>
    Task<PagedResult<ExpenseResponse>> GetAllAsync(ExpenseFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Gets a single expense by identifier (scoped to current user).
    /// </summary>
    Task<ExpenseResponse> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new expense for the authenticated user.
    /// </summary>
    Task<ExpenseResponse> CreateAsync(CreateExpenseRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing expense's fields.
    /// </summary>
    Task<ExpenseResponse> UpdateAsync(int id, UpdateExpenseRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes an expense owned by the authenticated user.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
