using LifeOS.Application.DTOs.Scores;
using LifeOS.Application.Queries.Scores;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/streaks")]
[Authorize]
public class StreaksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StreaksController> _logger;

    public StreaksController(IMediator mediator, ILogger<StreaksController> logger)
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
    /// Get all active streaks
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StreaksResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStreaks(
        [FromQuery] bool? isActive = true,
        [FromQuery] string? sort = "-currentStreakLength")
    {
        var result = await _mediator.Send(new GetStreaksQuery(GetUserId(), isActive, sort));
        return Ok(result);
    }
}
