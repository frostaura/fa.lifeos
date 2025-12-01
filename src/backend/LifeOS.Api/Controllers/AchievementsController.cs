using LifeOS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/achievements")]
[Authorize]
public class AchievementsController : ControllerBase
{
    private readonly IAchievementService _achievementService;
    private readonly ILogger<AchievementsController> _logger;
    
    public AchievementsController(
        IAchievementService achievementService,
        ILogger<AchievementsController> logger)
    {
        _achievementService = achievementService;
        _logger = logger;
    }
    
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get all achievements with unlock status for current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAchievements()
    {
        var userId = GetUserId();
        var achievements = await _achievementService.GetUserAchievementsAsync(userId);
        
        return Ok(new {
            data = achievements.Select(a => new {
                code = a.Code,
                name = a.Name,
                description = a.Description,
                icon = a.Icon,
                tier = a.Tier,
                xpValue = a.XpValue,
                unlocked = a.Unlocked,
                unlockedAt = a.UnlockedAt
            }),
            meta = new { 
                timestamp = DateTime.UtcNow,
                total = achievements.Count,
                unlocked = achievements.Count(a => a.Unlocked)
            }
        });
    }

    /// <summary>
    /// Get current user's XP and level information
    /// </summary>
    [HttpGet("xp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetXp()
    {
        var userId = GetUserId();
        var xp = await _achievementService.GetUserXpAsync(userId);
        
        return Ok(new {
            data = new {
                totalXp = xp.TotalXp,
                level = xp.Level,
                weeklyXp = xp.WeeklyXp,
                xpToNextLevel = xp.XpToNextLevel
            },
            meta = new { timestamp = DateTime.UtcNow }
        });
    }

    /// <summary>
    /// Evaluate and unlock any achievements the user has earned
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateAchievements()
    {
        var userId = GetUserId();
        var unlocked = await _achievementService.EvaluateAchievementsAsync(userId);
        
        return Ok(new {
            data = new {
                newlyUnlocked = unlocked.Select(u => new {
                    code = u.Achievement.Code,
                    name = u.Achievement.Name,
                    description = u.Achievement.Description,
                    icon = u.Achievement.Icon,
                    tier = u.Achievement.Tier,
                    xpAwarded = u.Achievement.XpValue,
                    context = u.Context
                })
            },
            meta = new { 
                timestamp = DateTime.UtcNow,
                count = unlocked.Count
            }
        });
    }
}
