using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Application.Abstractions;

/// <summary>
/// Provides CRUD operations for expense types scoped to the authenticated user.
/// </summary>
public interface IExpenseTypeService
{
    /// <summary>
    /// Lists expense types with optional name filter and pagination.
    /// </summary>
    Task<PagedResult<ExpenseTypeResponse>> GetAllAsync(ExpenseTypeFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Gets a single expense type by identifier (scoped to current user).
    /// </summary>
    Task<ExpenseTypeResponse> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new expense type for the authenticated user.
    /// </summary>
    Task<ExpenseTypeResponse> CreateAsync(CreateExpenseTypeRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing expense type's name.
    /// </summary>
    Task<ExpenseTypeResponse> UpdateAsync(int id, UpdateExpenseTypeRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes an expense type. Fails with conflict if expenses are linked.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
