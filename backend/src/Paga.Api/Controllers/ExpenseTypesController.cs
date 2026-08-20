using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paga.Application.Abstractions;
using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Api.Controllers;

/// <summary>
/// Manages expense type CRUD operations for the authenticated user.
/// </summary>
[ApiController]
[Route("api/expense-types")]
[Authorize]
public class ExpenseTypesController : ControllerBase
{
    private readonly IExpenseTypeService _expenseTypeService;

    public ExpenseTypesController(IExpenseTypeService expenseTypeService)
    {
        _expenseTypeService = expenseTypeService;
    }

    /// <summary>
    /// Lists expense types with optional name filter and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ExpenseTypeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? name,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var filter = new ExpenseTypeFilter(name, pageNumber, pageSize);
        var result = await _expenseTypeService.GetAllAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single expense type by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExpenseTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var expenseType = await _expenseTypeService.GetByIdAsync(id, ct);
        return Ok(expenseType);
    }

    /// <summary>
    /// Creates a new expense type for the authenticated user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseTypeRequest request, CancellationToken ct)
    {
        var expenseType = await _expenseTypeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = expenseType.Id }, expenseType);
    }

    /// <summary>
    /// Updates an existing expense type's name.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ExpenseTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseTypeRequest request, CancellationToken ct)
    {
        var expenseType = await _expenseTypeService.UpdateAsync(id, request, ct);
        return Ok(expenseType);
    }

    /// <summary>
    /// Deletes an expense type. Fails with 409 if expenses are linked.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _expenseTypeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
