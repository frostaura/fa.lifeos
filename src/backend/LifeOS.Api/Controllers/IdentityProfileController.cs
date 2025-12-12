using LifeOS.Application.Commands.Identity;
using LifeOS.Application.DTOs.Identity;
using LifeOS.Application.Queries.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/identity-profile")]
[Authorize]
public class IdentityProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IdentityProfileController> _logger;

    public IdentityProfileController(IMediator mediator, ILogger<IdentityProfileController> logger)
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
    /// Get user identity profile
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIdentityProfile()
    {
        var result = await _mediator.Send(new GetIdentityProfileQuery(GetUserId()));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Identity profile not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create or update identity profile
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateIdentityProfile([FromBody] UpdateIdentityProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Archetype))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Archetype is required" } });
        }

        var success = await _mediator.Send(new UpdateIdentityProfileCommand(
            GetUserId(),
            request.Archetype,
            request.ArchetypeDescription,
            request.Values,
            request.PrimaryStatTargets,
            request.LinkedMilestoneIds
        ));

        if (!success)
            return BadRequest(new { error = new { code = "UPDATE_FAILED", message = "Failed to update identity profile" } });

        _logger.LogInformation("Identity profile updated for user {UserId}", GetUserId());

        return Ok(new { success = true });
    }

    /// <summary>
    /// Update primary stat targets only
    /// </summary>
    [HttpPost("targets")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateIdentityTargets([FromBody] UpdateIdentityTargetsRequest request)
    {
        var success = await _mediator.Send(new UpdateIdentityTargetsCommand(
            GetUserId(),
            request.Targets
        ));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Identity profile not found. Create one first using PUT /api/identity-profile" } });

        _logger.LogInformation("Identity stat targets updated for user {UserId}", GetUserId());

        return Ok(new { success = true });
    }
}
