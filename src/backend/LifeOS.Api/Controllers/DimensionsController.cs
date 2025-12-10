using LifeOS.Application.Commands.Dimensions;
using LifeOS.Application.Queries.Dimensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/dimensions")]
[Authorize]
public class DimensionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DimensionsController> _logger;

    public DimensionsController(IMediator mediator, ILogger<DimensionsController> logger)
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
    /// Get all life dimensions with user weights
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDimensions()
    {
        var result = await _mediator.Send(new GetDimensionsQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Get single dimension with related milestones and tasks by code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDimensionByCode(string code)
    {
        // First get all dimensions to find the ID by code
        var dimensions = await _mediator.Send(new GetDimensionsQuery(GetUserId()));
        var dimension = dimensions.Data?.FirstOrDefault(d => 
            d.Attributes?.Code?.Equals(code, StringComparison.OrdinalIgnoreCase) == true);
        
        if (dimension == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Dimension not found" } });

        var result = await _mediator.Send(new GetDimensionByIdQuery(GetUserId(), dimension.Id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Dimension not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Get single dimension with related milestones and tasks by GUID
    /// </summary>
    [HttpGet("by-id/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDimensionById(Guid id)
    {
        var result = await _mediator.Send(new GetDimensionByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Dimension not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Update dimension weight
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDimensionWeight(Guid id, [FromBody] UpdateDimensionWeightRequest request)
    {
        var success = await _mediator.Send(new UpdateDimensionWeightCommand(
            id, 
            request.Weight, 
            request.AutoRebalance ?? true));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Dimension not found" } });

        var result = await _mediator.Send(new GetDimensionByIdQuery(GetUserId(), id));
        return Ok(result);
    }
}

public record UpdateDimensionWeightRequest
{
    public decimal Weight { get; init; }
    public bool? AutoRebalance { get; init; }
}
