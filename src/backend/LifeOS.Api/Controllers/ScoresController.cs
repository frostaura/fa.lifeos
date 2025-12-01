using LifeOS.Application.DTOs.Scores;
using LifeOS.Application.Queries.Scores;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/scores")]
[Authorize]
public class ScoresController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ScoresController> _logger;

    public ScoresController(IMediator mediator, ILogger<ScoresController> logger)
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
    /// Get all dimension scores + life score
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ScoresResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScores()
    {
        var result = await _mediator.Send(new GetScoresQuery(GetUserId()));
        return Ok(result);
    }
}
