using LifeOS.Application.Commands.Finances;
using LifeOS.Application.DTOs.FxRates;
using LifeOS.Application.Queries.FxRates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/fx-rates")]
[Authorize]
public class FxRatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FxRatesController> _logger;

    public FxRatesController(IMediator mediator, ILogger<FxRatesController> logger)
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
    /// Get current FX rates for all tracked currency pairs
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FxRateListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFxRates()
    {
        var result = await _mediator.Send(new GetFxRatesQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Trigger manual FX rate refresh
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(FxRateRefreshResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshFxRates()
    {
        var result = await _mediator.Send(new RefreshFxRatesCommand());
        return Ok(result);
    }
}
