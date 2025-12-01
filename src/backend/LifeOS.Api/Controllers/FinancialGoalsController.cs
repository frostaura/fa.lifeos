using LifeOS.Application.Commands.Finances;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Application.Queries.Finances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/financial-goals")]
[Authorize]
public class FinancialGoalsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FinancialGoalsController> _logger;

    public FinancialGoalsController(IMediator mediator, ILogger<FinancialGoalsController> logger)
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
    /// List financial goals with time-to-acquire calculations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FinancialGoalListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinancialGoals()
    {
        var result = await _mediator.Send(new GetFinancialGoalsQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Create financial goal
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateFinancialGoal([FromBody] CreateFinancialGoalRequest request)
    {
        var result = await _mediator.Send(new CreateFinancialGoalCommand(GetUserId(), request));
        return Created($"/api/financial-goals/{result.Id}", result);
    }

    /// <summary>
    /// Update financial goal
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFinancialGoal(Guid id, [FromBody] UpdateFinancialGoalRequest request)
    {
        var result = await _mediator.Send(new UpdateFinancialGoalCommand(GetUserId(), id, request));

        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Financial goal not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Delete financial goal
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFinancialGoal(Guid id)
    {
        var success = await _mediator.Send(new DeleteFinancialGoalCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Financial goal not found" } });

        return NoContent();
    }
}
