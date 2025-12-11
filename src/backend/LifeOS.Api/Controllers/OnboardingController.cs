using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<OnboardingController> _logger;

    private static readonly string[] OnboardingSteps = { "health_baselines", "major_goals", "identity" };

    public OnboardingController(ILifeOSDbContext context, ILogger<OnboardingController> logger)
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
    /// Get onboarding completion status
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnboardingStatus()
    {
        var userId = GetUserId();

        var user = await _context.Users.FindAsync(userId);
        var completedSteps = await _context.OnboardingResponses
            .Where(r => r.UserId == userId)
            .Select(r => r.StepCode)
            .ToListAsync();

        var stepsStatus = OnboardingSteps.Select(step => new
        {
            step,
            completed = completedSteps.Contains(step)
        }).ToList();

        return Ok(new
        {
            data = new
            {
                isComplete = user?.OnboardingCompleted ?? false,
                steps = stepsStatus,
                currentStep = stepsStatus.FirstOrDefault(s => !s.completed)?.step ?? "complete"
            }
        });
    }

    /// <summary>
    /// Submit health baselines
    /// </summary>
    [HttpPost("health-baselines")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitHealthBaselines([FromBody] HealthBaselinesRequest request)
    {
        var userId = GetUserId();

        if (request.CurrentWeight <= 0 || request.Height <= 0)
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Weight and height are required" } });
        }

        await SaveOnboardingResponseAsync(userId, "health_baselines", request);

        // Optionally record metric values
        var now = DateTime.UtcNow;
        var metricRecords = new List<MetricRecord>();

        if (request.CurrentWeight > 0)
        {
            metricRecords.Add(new MetricRecord
            {
                UserId = userId,
                MetricCode = "weight_kg",
                RecordedAt = now,
                ValueNumber = request.CurrentWeight,
                Source = "onboarding"
            });
        }

        if (request.CurrentBodyFat > 0)
        {
            metricRecords.Add(new MetricRecord
            {
                UserId = userId,
                MetricCode = "body_fat_pct",
                RecordedAt = now,
                ValueNumber = request.CurrentBodyFat.Value,
                Source = "onboarding"
            });
        }

        if (metricRecords.Any())
        {
            _context.MetricRecords.AddRange(metricRecords);
        }

        // Update user's date of birth if provided
        if (request.BirthDate.HasValue)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.DateOfBirth = request.BirthDate.Value;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Health baselines submitted for user {UserId}", userId);

        return Ok(new { data = new { step = "health_baselines", completed = true } });
    }

    /// <summary>
    /// Submit major goals
    /// </summary>
    [HttpPost("major-goals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitMajorGoals([FromBody] MajorGoalsRequest request)
    {
        var userId = GetUserId();

        await SaveOnboardingResponseAsync(userId, "major_goals", request);

        // Create financial goals
        if (request.FinancialGoals?.Any() == true)
        {
            var user = await _context.Users.FindAsync(userId);
            var userAge = user?.DateOfBirth != null
                ? DateTime.UtcNow.Year - user.DateOfBirth.Value.Year
                : 30;

            foreach (var goal in request.FinancialGoals)
            {
                var targetDate = DateTime.UtcNow.AddYears(goal.TargetAge - userAge);
                var financialGoal = new FinancialGoal
                {
                    UserId = userId,
                    Name = goal.Description,
                    TargetAmount = goal.TargetAmount,
                    TargetDate = targetDate,
                    IsActive = true
                };
                _context.FinancialGoals.Add(financialGoal);
            }
        }

        // Create life milestones
        if (request.LifeMilestones?.Any() == true)
        {
            var dimensions = await _context.Dimensions.ToDictionaryAsync(d => d.Code.ToLower(), d => d.Id);

            foreach (var milestone in request.LifeMilestones)
            {
                var dimensionId = dimensions.GetValueOrDefault(milestone.Dimension?.ToLower() ?? "growth");
                var newMilestone = new Milestone
                {
                    UserId = userId,
                    DimensionId = dimensionId,
                    Title = milestone.Description,
                    TargetDate = milestone.TargetDate,
                    Status = Domain.Enums.MilestoneStatus.Active
                };
                _context.Milestones.Add(newMilestone);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Major goals submitted for user {UserId}", userId);

        return Ok(new { data = new { step = "major_goals", completed = true } });
    }

    /// <summary>
    /// Submit identity traits
    /// </summary>
    [HttpPost("identity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitIdentity([FromBody] IdentityRequest request)
    {
        var userId = GetUserId();

        await SaveOnboardingResponseAsync(userId, "identity", request);

        // Create or update identity profile
        var profile = await _context.IdentityProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        var defaultTargets = new Dictionary<string, int>
        {
            { "strength", 75 },
            { "wisdom", 75 },
            { "charisma", 75 },
            { "composure", 75 },
            { "energy", 75 },
            { "influence", 75 },
            { "vitality", 75 }
        };

        // Boost targeted stats
        if (request.PrimaryStatFocus?.Any() == true)
        {
            foreach (var stat in request.PrimaryStatFocus)
            {
                if (defaultTargets.ContainsKey(stat.ToLower()))
                {
                    defaultTargets[stat.ToLower()] = 90;
                }
            }
        }

        if (profile == null)
        {
            profile = new IdentityProfile
            {
                UserId = userId,
                Archetype = request.Archetype ?? "Balanced Achiever",
                ArchetypeDescription = $"Focused on: {string.Join(", ", request.PrimaryStatFocus ?? new List<string>())}",
                Values = JsonSerializer.Serialize(request.Values ?? new List<string>()),
                PrimaryStatTargets = JsonSerializer.Serialize(defaultTargets),
                LinkedMilestoneIds = "[]"
            };
            _context.IdentityProfiles.Add(profile);
        }
        else
        {
            profile.Archetype = request.Archetype ?? profile.Archetype;
            profile.Values = JsonSerializer.Serialize(request.Values ?? new List<string>());
            profile.PrimaryStatTargets = JsonSerializer.Serialize(defaultTargets);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Identity traits submitted for user {UserId}", userId);

        return Ok(new { data = new { step = "identity", completed = true } });
    }

    /// <summary>
    /// Mark onboarding as complete
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteOnboarding()
    {
        var userId = GetUserId();

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.OnboardingCompleted = true;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Onboarding completed for user {UserId}", userId);

        return Ok(new { data = new { isComplete = true } });
    }

    private async Task SaveOnboardingResponseAsync(Guid userId, string stepCode, object data)
    {
        // Remove existing response for this step
        var existing = await _context.OnboardingResponses
            .FirstOrDefaultAsync(r => r.UserId == userId && r.StepCode == stepCode);

        if (existing != null)
        {
            _context.OnboardingResponses.Remove(existing);
        }

        var response = new OnboardingResponse
        {
            UserId = userId,
            StepCode = stepCode,
            ResponseData = JsonSerializer.Serialize(data),
            CompletedAt = DateTime.UtcNow
        };

        _context.OnboardingResponses.Add(response);
    }
}

public record HealthBaselinesRequest
{
    public decimal CurrentWeight { get; init; }
    public decimal TargetWeight { get; init; }
    public decimal? CurrentBodyFat { get; init; }
    public decimal? TargetBodyFat { get; init; }
    public decimal Height { get; init; }
    public DateOnly? BirthDate { get; init; }
}

public record MajorGoalsRequest
{
    public List<FinancialGoalInput>? FinancialGoals { get; init; }
    public List<LifeMilestoneInput>? LifeMilestones { get; init; }
}

public record FinancialGoalInput
{
    public string Description { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public int TargetAge { get; init; }
}

public record LifeMilestoneInput
{
    public string Description { get; init; } = string.Empty;
    public DateOnly? TargetDate { get; init; }
    public string? Dimension { get; init; }
}

public record IdentityRequest
{
    public string? Archetype { get; init; }
    public List<string>? Values { get; init; }
    public List<string>? PrimaryStatFocus { get; init; }
}
