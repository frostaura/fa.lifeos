using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/identity-profile")]
[Authorize]
public class IdentityProfileController : ControllerBase
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<IdentityProfileController> _logger;

    public IdentityProfileController(ILifeOSDbContext context, ILogger<IdentityProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get the user's identity profile
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIdentityProfile()
    {
        var userId = GetUserId();
        
        var profile = await _context.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Identity profile not found" } });
        }

        // Get linked milestones
        var milestoneIds = JsonSerializer.Deserialize<List<Guid>>(profile.LinkedMilestoneIds) ?? new List<Guid>();
        var milestones = await _context.Milestones
            .Where(m => milestoneIds.Contains(m.Id) && m.UserId == userId)
            .Select(m => new { m.Id, m.Title })
            .ToListAsync();

        return Ok(new
        {
            data = new
            {
                archetype = profile.Archetype,
                archetypeDescription = profile.ArchetypeDescription,
                values = JsonSerializer.Deserialize<List<string>>(profile.Values),
                primaryStatTargets = JsonSerializer.Deserialize<Dictionary<string, int>>(profile.PrimaryStatTargets),
                linkedMilestones = milestones
            }
        });
    }

    /// <summary>
    /// Create or update the user's identity profile
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertIdentityProfile([FromBody] IdentityProfileRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(request.Archetype))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Archetype is required" } });
        }

        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var valuesJson = JsonSerializer.Serialize(request.Values ?? new List<string>());
        var targetsJson = JsonSerializer.Serialize(request.PrimaryStatTargets ?? new Dictionary<string, int>());
        var milestonesJson = JsonSerializer.Serialize(request.LinkedMilestoneIds ?? new List<Guid>());

        if (profile == null)
        {
            profile = new IdentityProfile
            {
                UserId = userId,
                Archetype = request.Archetype,
                ArchetypeDescription = request.ArchetypeDescription,
                Values = valuesJson,
                PrimaryStatTargets = targetsJson,
                LinkedMilestoneIds = milestonesJson
            };
            _context.IdentityProfiles.Add(profile);
        }
        else
        {
            profile.Archetype = request.Archetype;
            profile.ArchetypeDescription = request.ArchetypeDescription;
            profile.Values = valuesJson;
            profile.PrimaryStatTargets = targetsJson;
            profile.LinkedMilestoneIds = milestonesJson;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Identity profile updated for user {UserId}", userId);

        return Ok(new
        {
            data = new
            {
                archetype = profile.Archetype,
                archetypeDescription = profile.ArchetypeDescription,
                values = request.Values,
                primaryStatTargets = request.PrimaryStatTargets,
                linkedMilestoneIds = request.LinkedMilestoneIds
            }
        });
    }
}

public record IdentityProfileRequest
{
    public string Archetype { get; init; } = string.Empty;
    public string? ArchetypeDescription { get; init; }
    public List<string>? Values { get; init; }
    public Dictionary<string, int>? PrimaryStatTargets { get; init; }
    public List<Guid>? LinkedMilestoneIds { get; init; }
}
