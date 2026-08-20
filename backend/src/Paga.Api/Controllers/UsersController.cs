using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paga.Application.Abstractions;
using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Api.Controllers;

/// <summary>
/// Administrative CRUD operations for system users.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Lists users with optional name/email filter and pagination.
    /// </summary>
    /// <param name="name">Optional case-insensitive name filter (contains match).</param>
    /// <param name="email">Optional case-insensitive email filter (contains match).</param>
    /// <param name="pageNumber">Page number (1-based, defaults to 1).</param>
    /// <param name="pageSize">Page size (defaults to 10, maximum 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of users.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? email,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var filter = new UserFilter(name, email, pageNumber, pageSize);
        var result = await _userService.GetAllAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a user by unique identifier.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user data.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        return Ok(user);
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">User creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created user data with HTTP 201 and Location header.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// Updates an existing user. Password is optional — when null or empty, the current hash
    /// remains unchanged.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="request">Updated user data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user data.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _userService.UpdateAsync(id, request, ct);
        return Ok(user);
    }

    /// <summary>
    /// Deletes a user and all related data by cascade.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 204 No Content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _userService.DeleteAsync(id, ct);
        return NoContent();
    }
}
