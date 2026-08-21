using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paga.Application.Abstractions;
using Paga.Application.Common;
using Paga.Application.DTOs;

namespace Paga.Api.Controllers;

/// <summary>
/// Manages income CRUD operations for the authenticated user.
/// </summary>
[ApiController]
[Route("api/incomes")]
[Authorize]
public class IncomesController : ControllerBase
{
    private readonly IIncomeService _incomeService;

    public IncomesController(IIncomeService incomeService)
    {
        _incomeService = incomeService;
    }

    /// <summary>
    /// Lists incomes with optional date range, description, and recurrence filters plus pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IncomeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] string? description,
        [FromQuery] bool? isRecurring,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var filter = new IncomeFilter(dateFrom, dateTo, description, isRecurring, pageNumber, pageSize);
        var result = await _incomeService.GetAllAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single income by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var income = await _incomeService.GetByIdAsync(id, ct);
        return Ok(income);
    }

    /// <summary>
    /// Creates a new income for the authenticated user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateIncomeRequest request, CancellationToken ct)
    {
        var income = await _incomeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = income.Id }, income);
    }

    /// <summary>
    /// Updates an existing income's fields.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateIncomeRequest request, CancellationToken ct)
    {
        var income = await _incomeService.UpdateAsync(id, request, ct);
        return Ok(income);
    }

    /// <summary>
    /// Deletes an income owned by the authenticated user.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _incomeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
