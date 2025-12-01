using LifeOS.Application.Commands.Milestones;
using LifeOS.Application.DTOs.Milestones;
using LifeOS.Application.Queries.Milestones;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/milestones")]
[Authorize]
public class MilestonesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MilestonesController> _logger;

    public MilestonesController(IMediator mediator, ILogger<MilestonesController> logger)
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
    /// List milestones with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMilestones(
        [FromQuery] Guid? dimensionId,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var result = await _mediator.Send(new GetMilestonesQuery(
            GetUserId(),
            dimensionId,
            status,
            sort,
            page,
            Math.Min(perPage, 100)));

        return Ok(result);
    }

    /// <summary>
    /// Get milestone with linked tasks
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMilestoneById(Guid id)
    {
        var result = await _mediator.Send(new GetMilestoneByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Milestone not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create milestone
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateMilestone([FromBody] CreateMilestoneRequest request)
    {
        var result = await _mediator.Send(new CreateMilestoneCommand(
            GetUserId(),
            request.Title,
            request.Description,
            request.DimensionId,
            request.TargetDate,
            request.TargetMetricCode,
            request.TargetMetricValue));

        return CreatedAtAction(nameof(GetMilestoneById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update milestone
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMilestone(Guid id, [FromBody] UpdateMilestoneRequest request)
    {
        var success = await _mediator.Send(new UpdateMilestoneCommand(
            GetUserId(),
            id,
            request.Title,
            request.Description,
            request.TargetDate,
            request.Status));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Milestone not found" } });

        var result = await _mediator.Send(new GetMilestoneByIdQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Delete milestone
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMilestone(Guid id)
    {
        var success = await _mediator.Send(new DeleteMilestoneCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Milestone not found" } });

        return NoContent();
    }
}
