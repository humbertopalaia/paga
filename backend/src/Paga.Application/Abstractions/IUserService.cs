using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Application.Abstractions;

/// <summary>
/// Provides administrative CRUD operations for users.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Lists users with optional name/email filter and pagination.
    /// </summary>
    Task<PagedResult<UserResponse>> GetAllAsync(UserFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Gets a single user by unique identifier.
    /// </summary>
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new user with the provided data.
    /// </summary>
    Task<UserResponse> CreateAsync(CreateUserRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user's data, optionally resetting the password.
    /// </summary>
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a user and all related data by cascade.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
