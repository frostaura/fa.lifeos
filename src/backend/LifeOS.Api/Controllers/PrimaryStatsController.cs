using LifeOS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/primary-stats")]
[Authorize]
public class PrimaryStatsController : ControllerBase
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<PrimaryStatsController> _logger;

    // Dimension-to-Stat mapping (v1.1 spec)
    private static readonly Dictionary<string, string[]> DimensionStatMapping = new()
    {
        { "health_recovery", new[] { "vitality", "energy", "composure" } },
        { "health", new[] { "vitality", "energy", "composure" } }, // Legacy support
        { "relationships", new[] { "charisma", "influence", "composure" } },
        { "work_contribution", new[] { "wisdom", "influence", "energy" } },
        { "work", new[] { "wisdom", "influence", "energy" } }, // Legacy support
        { "play_adventure", new[] { "energy", "vitality", "charisma" } },
        { "play", new[] { "energy", "vitality", "charisma" } }, // Legacy support
        { "asset_care", new[] { "wisdom", "composure" } },
        { "assets", new[] { "wisdom", "composure" } }, // Legacy support
        { "create_craft", new[] { "wisdom", "energy", "influence" } },
        { "create", new[] { "wisdom", "energy", "influence" } }, // Legacy support
        { "growth_mind", new[] { "wisdom", "strength", "composure" } },
        { "growth", new[] { "wisdom", "strength", "composure" } }, // Legacy support
        { "community_meaning", new[] { "charisma", "influence", "vitality" } },
        { "community", new[] { "charisma", "influence", "vitality" } } // Legacy support
    };

    public PrimaryStatsController(ILifeOSDbContext context, ILogger<PrimaryStatsController> logger)
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
    /// Get current primary stats with targets
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrimaryStats()
    {
        var userId = GetUserId();

        // Get latest primary stat record
        var latestRecord = await _context.PrimaryStatRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync();

        // Get identity profile for targets
        var profile = await _context.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var targets = profile != null
            ? JsonSerializer.Deserialize<Dictionary<string, int>>(profile.PrimaryStatTargets)
            : GetDefaultTargets();

        // If no record exists, calculate current stats
        Dictionary<string, int> currentStats;
        Dictionary<string, object>? breakdown = null;

        if (latestRecord != null)
        {
            currentStats = new Dictionary<string, int>
            {
                { "strength", latestRecord.Strength },
                { "wisdom", latestRecord.Wisdom },
                { "charisma", latestRecord.Charisma },
                { "composure", latestRecord.Composure },
                { "energy", latestRecord.Energy },
                { "influence", latestRecord.Influence },
                { "vitality", latestRecord.Vitality }
            };

            if (!string.IsNullOrEmpty(latestRecord.CalculationDetails))
            {
                breakdown = JsonSerializer.Deserialize<Dictionary<string, object>>(latestRecord.CalculationDetails);
            }
        }
        else
        {
            // Calculate stats from dimension scores
            var (stats, calcBreakdown) = await CalculateCurrentStatsAsync(userId);
            currentStats = stats;
            breakdown = calcBreakdown;
        }

        return Ok(new
        {
            data = new
            {
                currentStats,
                targets,
                calculatedAt = latestRecord?.RecordedAt ?? DateTime.UtcNow,
                breakdown
            }
        });
    }

    /// <summary>
    /// Get primary stats history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrimaryStatsHistory([FromQuery] int days = 30)
    {
        var userId = GetUserId();
        var since = DateTime.UtcNow.AddDays(-days);

        var records = await _context.PrimaryStatRecords
            .Where(r => r.UserId == userId && r.RecordedAt >= since)
            .OrderBy(r => r.RecordedAt)
            .Select(r => new
            {
                recordedAt = r.RecordedAt,
                strength = r.Strength,
                wisdom = r.Wisdom,
                charisma = r.Charisma,
                composure = r.Composure,
                energy = r.Energy,
                influence = r.Influence,
                vitality = r.Vitality
            })
            .ToListAsync();

        return Ok(new { data = records });
    }

    /// <summary>
    /// Recalculate and store current primary stats
    /// </summary>
    [HttpPost("recalculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecalculatePrimaryStats()
    {
        var userId = GetUserId();

        var (stats, breakdown) = await CalculateCurrentStatsAsync(userId);

        var record = new Domain.Entities.PrimaryStatRecord
        {
            UserId = userId,
            RecordedAt = DateTime.UtcNow,
            Strength = stats["strength"],
            Wisdom = stats["wisdom"],
            Charisma = stats["charisma"],
            Composure = stats["composure"],
            Energy = stats["energy"],
            Influence = stats["influence"],
            Vitality = stats["vitality"],
            CalculationDetails = JsonSerializer.Serialize(breakdown)
        };

        _context.PrimaryStatRecords.Add(record);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Primary stats recalculated for user {UserId}", userId);

        return Ok(new
        {
            data = new
            {
                currentStats = stats,
                calculatedAt = record.RecordedAt,
                breakdown
            }
        });
    }

    private async Task<(Dictionary<string, int> stats, Dictionary<string, object> breakdown)> CalculateCurrentStatsAsync(Guid userId)
    {
        // Get dimension scores
        var dimensions = await _context.Dimensions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync();

        // Get latest score records per dimension
        var dimensionScores = new Dictionary<string, int>();
        foreach (var dim in dimensions)
        {
            // For now, use a placeholder score of 50 per dimension
            // In a full implementation, this would query ScoreRecords
            dimensionScores[dim.Code] = 50;
        }

        // Calculate each primary stat
        var statContributions = new Dictionary<string, List<(string dimension, int score)>>
        {
            { "strength", new() },
            { "wisdom", new() },
            { "charisma", new() },
            { "composure", new() },
            { "energy", new() },
            { "influence", new() },
            { "vitality", new() }
        };

        foreach (var (dimCode, score) in dimensionScores)
        {
            if (DimensionStatMapping.TryGetValue(dimCode.ToLowerInvariant(), out var stats))
            {
                foreach (var stat in stats)
                {
                    if (statContributions.ContainsKey(stat))
                    {
                        statContributions[stat].Add((dimCode, score));
                    }
                }
            }
        }

        var finalStats = new Dictionary<string, int>();
        var breakdown = new Dictionary<string, object>();

        foreach (var (stat, contributions) in statContributions)
        {
            if (contributions.Count == 0)
            {
                finalStats[stat] = 50; // Default
            }
            else
            {
                var avg = (int)Math.Round(contributions.Average(c => c.score));
                finalStats[stat] = Math.Clamp(avg, 0, 100);
            }

            breakdown[stat] = new
            {
                fromDimensions = contributions.ToDictionary(c => c.dimension, c => c.score),
                weighted = finalStats[stat]
            };
        }

        return (finalStats, breakdown);
    }

    private static Dictionary<string, int> GetDefaultTargets()
    {
        return new Dictionary<string, int>
        {
            { "strength", 75 },
            { "wisdom", 75 },
            { "charisma", 75 },
            { "composure", 75 },
            { "energy", 75 },
            { "influence", 75 },
            { "vitality", 75 }
        };
    }
}
