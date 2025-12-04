using LifeOS.Application.Commands.Finances;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Application.Queries.Finances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/expense-definitions")]
[Authorize]
public class ExpenseDefinitionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExpenseDefinitionsController> _logger;

    public ExpenseDefinitionsController(IMediator mediator, ILogger<ExpenseDefinitionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// List expense definitions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ExpenseDefinitionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenseDefinitions()
    {
        var result = await _mediator.Send(new GetExpenseDefinitionsQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Create expense definition
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseDefinitionDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateExpenseDefinition([FromBody] CreateExpenseDefinitionRequest request)
    {
        var result = await _mediator.Send(new CreateExpenseDefinitionCommand(
            GetUserId(),
            request.Name,
            request.Currency,
            request.AmountType,
            request.AmountValue,
            request.AmountFormula,
            request.Frequency,
            request.StartDate,
            request.Category,
            request.IsTaxDeductible,
            request.LinkedAccountId,
            request.InflationAdjusted,
            request.EndConditionType,
            request.EndConditionAccountId,
            request.EndDate,
            request.EndAmountThreshold));

        return Created($"/api/expense-definitions/{result.Data.Id}", result);
    }

    /// <summary>
    /// Update expense definition
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseDefinitionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExpenseDefinition(Guid id, [FromBody] UpdateExpenseDefinitionRequest request)
    {
        var success = await _mediator.Send(new UpdateExpenseDefinitionCommand(
            GetUserId(),
            id,
            request.Name,
            request.AmountValue,
            request.AmountFormula,
            request.Frequency,
            request.StartDate,
            request.Category,
            request.IsTaxDeductible,
            request.LinkedAccountId,
            request.InflationAdjusted,
            request.IsActive,
            request.EndConditionType,
            request.EndConditionAccountId,
            request.EndDate,
            request.EndAmountThreshold));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Expense definition not found" } });

        var result = await _mediator.Send(new GetExpenseDefinitionsQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Delete expense definition
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpenseDefinition(Guid id)
    {
        var success = await _mediator.Send(new DeleteExpenseDefinitionCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Expense definition not found" } });

        return NoContent();
    }
}
