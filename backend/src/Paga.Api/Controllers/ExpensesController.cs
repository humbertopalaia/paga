using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paga.Application.Abstractions;
using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Api.Controllers;

/// <summary>
/// Manages expense CRUD operations for the authenticated user.
/// </summary>
[ApiController]
[Route("api/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Lists expenses with optional due date range, expense type, description, and recurrence filters plus pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? dueDateFrom,
        [FromQuery] DateOnly? dueDateTo,
        [FromQuery] int? expenseTypeId,
        [FromQuery] string? description,
        [FromQuery] bool? isRecurring,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var filter = new ExpenseFilter(dueDateFrom, dueDateTo, expenseTypeId, description, isRecurring, pageNumber, pageSize);
        var result = await _expenseService.GetAllAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single expense by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var expense = await _expenseService.GetByIdAsync(id, ct);
        return Ok(expense);
    }

    /// <summary>
    /// Creates a new expense for the authenticated user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var expense = await _expenseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
    }

    /// <summary>
    /// Updates an existing expense's fields.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseRequest request, CancellationToken ct)
    {
        var expense = await _expenseService.UpdateAsync(id, request, ct);
        return Ok(expense);
    }

    /// <summary>
    /// Deletes an expense owned by the authenticated user.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _expenseService.DeleteAsync(id, ct);
        return NoContent();
    }
}
