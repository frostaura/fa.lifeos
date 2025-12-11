using LifeOS.Application.DTOs.Scores;
using LifeOS.Application.Queries.Scores;
using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/scores")]
[Authorize]
public class ScoresController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ScoresController> _logger;
    private readonly ILifeOSDbContext _context;

    public ScoresController(IMediator mediator, ILogger<ScoresController> logger, ILifeOSDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _context = context;
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

    #region v1.1 Scientific Scoring

    /// <summary>
    /// v1.1: Get Health Index (metric-based health score)
    /// </summary>
    [HttpGet("health-index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthIndex()
    {
        var userId = GetUserId();
        
        // Get recent health metrics
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var healthMetrics = await _context.MetricRecords
            .Where(m => m.UserId == userId && m.RecordedAt >= thirtyDaysAgo)
            .Where(m => m.MetricCode == "weight_kg" || m.MetricCode == "body_fat_pct" || 
                       m.MetricCode == "sleep_hours" || m.MetricCode == "steps_count" ||
                       m.MetricCode == "resting_heart_rate")
            .OrderByDescending(m => m.RecordedAt)
            .ToListAsync();

        var latestWeight = healthMetrics.FirstOrDefault(m => m.MetricCode == "weight_kg")?.ValueNumber;
        var latestBodyFat = healthMetrics.FirstOrDefault(m => m.MetricCode == "body_fat_pct")?.ValueNumber;
        
        var sleepMetrics = healthMetrics.Where(m => m.MetricCode == "sleep_hours").Take(7).ToList();
        var avgSleep = sleepMetrics.Any() ? sleepMetrics.Average(m => m.ValueNumber ?? 0) : 0m;
        
        var stepsMetrics = healthMetrics.Where(m => m.MetricCode == "steps_count").Take(7).ToList();
        var avgSteps = stepsMetrics.Any() ? stepsMetrics.Average(m => m.ValueNumber ?? 0) : 0m;

        // Calculate component scores (0-100)
        var bmiScore = CalculateBmiScore(latestWeight, 180); // Assume height 180cm for now
        var bodyFatScore = CalculateBodyFatScore(latestBodyFat);
        var sleepScore = CalculateSleepScore(avgSleep);
        var activityScore = CalculateActivityScore(avgSteps);

        // Weighted average
        var healthIndex = (bmiScore * 0.25m) + (bodyFatScore * 0.25m) + 
                         (sleepScore * 0.25m) + (activityScore * 0.25m);

        return Ok(new
        {
            data = new
            {
                healthIndex = Math.Round(healthIndex, 1),
                components = new
                {
                    bmiScore = Math.Round(bmiScore, 1),
                    bodyFatScore = Math.Round(bodyFatScore, 1),
                    sleepScore = Math.Round(sleepScore, 1),
                    activityScore = Math.Round(activityScore, 1)
                },
                metrics = new
                {
                    latestWeight,
                    latestBodyFat,
                    avgSleepHours = Math.Round(avgSleep, 1),
                    avgDailySteps = (int)avgSteps
                },
                calculatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// v1.1: Get Behavioral Adherence Index (task/habit completion rate)
    /// </summary>
    [HttpGet("adherence-index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdherenceIndex()
    {
        var userId = GetUserId();

        // Get task completion stats
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync();

        var totalRecurring = tasks.Count(t => t.Frequency != Domain.Enums.Frequency.AdHoc);
        
        // Get streaks for adherence calculation
        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var avgStreakLength = streaks.Any() ? streaks.Average(s => s.CurrentStreakLength) : 0;
        var missedPenaltyTotal = streaks.Sum(s => s.RiskPenaltyScore);

        // Calculate adherence components
        var streakScore = (decimal)Math.Min(100, avgStreakLength * 10); // 10 days = 100%
        var penaltyScore = (decimal)Math.Max(0, 100 - missedPenaltyTotal);
        
        var adherenceIndex = (streakScore * 0.6m) + (penaltyScore * 0.4m);

        return Ok(new
        {
            data = new
            {
                adherenceIndex = Math.Round(adherenceIndex, 1),
                components = new
                {
                    streakScore = Math.Round(streakScore, 1),
                    penaltyScore = Math.Round(penaltyScore, 1)
                },
                stats = new
                {
                    totalRecurringTasks = totalRecurring,
                    averageStreakDays = Math.Round(avgStreakLength, 1),
                    totalPenaltyPoints = missedPenaltyTotal
                },
                calculatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// v1.1: Get Wealth Health Score (financial health indicator)
    /// </summary>
    [HttpGet("wealth-health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWealthHealth()
    {
        var userId = GetUserId();

        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsLiability).Sum(a => Math.Abs(a.CurrentBalance));
        var netWorth = totalAssets - totalLiabilities;

        // Wealth health components
        var debtRatio = totalAssets > 0 ? (totalLiabilities / totalAssets) * 100 : 100;
        var debtScore = Math.Max(0, 100 - debtRatio);

        var emergencyFund = accounts
            .Where(a => a.AccountType == Domain.Enums.AccountType.Bank && !a.IsLiability)
            .Sum(a => a.CurrentBalance);
        var monthlyExpenses = 50000m; // Placeholder - would come from expense definitions
        var emergencyMonths = monthlyExpenses > 0 ? emergencyFund / monthlyExpenses : 0;
        var emergencyScore = Math.Min(100, emergencyMonths * 16.67m); // 6 months = 100

        var investmentRatio = totalAssets > 0 
            ? (accounts.Where(a => a.AccountType == Domain.Enums.AccountType.Investment)
                .Sum(a => a.CurrentBalance) / totalAssets) * 100 
            : 0;
        var investmentScore = Math.Min(100, investmentRatio * 2); // 50% invested = 100

        var wealthHealthScore = (debtScore * 0.4m) + (emergencyScore * 0.3m) + (investmentScore * 0.3m);

        return Ok(new
        {
            data = new
            {
                wealthHealthScore = Math.Round(wealthHealthScore, 1),
                components = new
                {
                    debtScore = Math.Round(debtScore, 1),
                    emergencyFundScore = Math.Round(emergencyScore, 1),
                    investmentScore = Math.Round(investmentScore, 1)
                },
                metrics = new
                {
                    netWorth,
                    totalAssets,
                    totalLiabilities,
                    debtToAssetRatio = Math.Round(debtRatio, 1),
                    emergencyFundMonths = Math.Round(emergencyMonths, 1),
                    investmentPercentage = Math.Round(investmentRatio, 1)
                },
                calculatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// v1.1: Get Longevity Years Added (risk-based life expectancy adjustment)
    /// </summary>
    [HttpGet("longevity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLongevityScore()
    {
        var userId = GetUserId();

        var user = await _context.Users.FindAsync(userId);
        var baseLifeExpectancy = user?.LifeExpectancyBaseline ?? 80m;

        // Get longevity models (global models, not user-specific)
        var longevityModels = await _context.LongevityModels
            .Where(m => m.IsActive)
            .ToListAsync();

        // Calculate cumulative years added based on model parameters
        decimal totalYearsAdded = 0;
        var modelContributions = new List<object>();

        foreach (var model in longevityModels)
        {
            // Use a base score of 50 (neutral) for now
            var baseScore = 50m;
            var yearsAdded = (baseScore / 100m) * 2m; // Each model can add up to 2 years at 100%
            totalYearsAdded += yearsAdded;
            modelContributions.Add(new
            {
                name = model.Name,
                code = model.Code,
                estimatedYears = Math.Round(yearsAdded, 2)
            });
        }

        // Cap at reasonable bounds
        totalYearsAdded = Math.Max(-20, Math.Min(20, totalYearsAdded));
        var adjustedLifeExpectancy = baseLifeExpectancy + totalYearsAdded;

        return Ok(new
        {
            data = new
            {
                baseLifeExpectancy,
                adjustedLifeExpectancy = Math.Round(adjustedLifeExpectancy, 1),
                yearsAdded = Math.Round(totalYearsAdded, 1),
                modelContributions,
                calculatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// v1.1: Get Composite LifeOS Score (combination of all scores)
    /// </summary>
    [HttpGet("composite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompositeScore()
    {
        var userId = GetUserId();

        // Get dimension scores
        var dimensionScores = await _mediator.Send(new GetScoresQuery(userId));
        
        // Calculate average life score from dimension scores
        var lifeScore = dimensionScores?.Data?.Any() == true 
            ? dimensionScores.Data.Average(d => d.Attributes.CurrentValue) 
            : 50m;

        // Calculate other indices (simplified for composite)
        var healthIndex = 50m; // Would call GetHealthIndex internally
        var adherenceIndex = 50m; // Would call GetAdherenceIndex internally
        var wealthHealth = 50m; // Would call GetWealthHealth internally

        // Composite weighted average
        var compositeScore = (lifeScore * 0.4m) + (healthIndex * 0.2m) + 
                            (adherenceIndex * 0.2m) + (wealthHealth * 0.2m);

        return Ok(new
        {
            data = new
            {
                compositeScore = Math.Round(compositeScore, 1),
                components = new
                {
                    lifeScore = Math.Round(lifeScore, 1),
                    healthIndex = Math.Round(healthIndex, 1),
                    adherenceIndex = Math.Round(adherenceIndex, 1),
                    wealthHealth = Math.Round(wealthHealth, 1)
                },
                weights = new
                {
                    lifeScore = 0.4,
                    healthIndex = 0.2,
                    adherenceIndex = 0.2,
                    wealthHealth = 0.2
                },
                calculatedAt = DateTime.UtcNow
            }
        });
    }

    #endregion

    #region Helper Methods

    private static decimal CalculateBmiScore(decimal? weight, decimal heightCm)
    {
        if (!weight.HasValue || heightCm <= 0) return 50;
        
        var heightM = heightCm / 100;
        var bmi = weight.Value / (heightM * heightM);
        
        // Optimal BMI is 18.5-24.9
        if (bmi >= 18.5m && bmi <= 24.9m) return 100;
        if (bmi >= 25m && bmi <= 29.9m) return 70; // Overweight
        if (bmi >= 30m && bmi <= 34.9m) return 50; // Obese class 1
        if (bmi < 18.5m) return 60; // Underweight
        return 30; // Obese class 2+
    }

    private static decimal CalculateBodyFatScore(decimal? bodyFat)
    {
        if (!bodyFat.HasValue) return 50;
        
        // Optimal range for men: 10-20%, women: 18-28% (using male defaults)
        if (bodyFat.Value <= 15) return 100;
        if (bodyFat.Value <= 20) return 85;
        if (bodyFat.Value <= 25) return 70;
        if (bodyFat.Value <= 30) return 50;
        return 30;
    }

    private static decimal CalculateSleepScore(decimal avgSleep)
    {
        // Optimal: 7-9 hours
        if (avgSleep >= 7 && avgSleep <= 9) return 100;
        if (avgSleep >= 6 && avgSleep < 7) return 75;
        if (avgSleep > 9 && avgSleep <= 10) return 80;
        if (avgSleep >= 5 && avgSleep < 6) return 50;
        return 30;
    }

    private static decimal CalculateActivityScore(decimal avgSteps)
    {
        // Optimal: 10,000+ steps
        if (avgSteps >= 10000) return 100;
        if (avgSteps >= 7500) return 85;
        if (avgSteps >= 5000) return 70;
        if (avgSteps >= 2500) return 50;
        return 30;
    }

    #endregion

    #region v1.2 Comprehensive Scoring

    /// <summary>
    /// v1.2: Calculate and return Health Index snapshot
    /// </summary>
    [HttpPost("health-index/calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateHealthIndex([FromServices] LifeOS.Application.Services.IHealthIndexService healthIndexService)
    {
        var userId = GetUserId();
        var snapshot = await healthIndexService.CalculateHealthIndexAsync(userId);
        
        return Ok(new
        {
            data = new
            {
                score = snapshot.Score,
                components = System.Text.Json.JsonSerializer.Deserialize<object>(snapshot.Components),
                timestamp = snapshot.Timestamp
            }
        });
    }

    /// <summary>
    /// v1.2: Calculate and return Behavioral Adherence snapshot
    /// </summary>
    [HttpPost("adherence/calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateAdherence(
        [FromServices] LifeOS.Application.Services.IAdherenceService adherenceService,
        [FromQuery] int timeWindowDays = 7)
    {
        var userId = GetUserId();
        var snapshot = await adherenceService.CalculateAdherenceAsync(userId, timeWindowDays);
        
        return Ok(new
        {
            data = new
            {
                score = snapshot.Score,
                timeWindowDays = snapshot.TimeWindowDays,
                tasksConsidered = snapshot.TasksConsidered,
                tasksCompleted = snapshot.TasksCompleted,
                completionRate = snapshot.TasksConsidered > 0 
                    ? (decimal)snapshot.TasksCompleted / snapshot.TasksConsidered 
                    : 1m,
                timestamp = snapshot.Timestamp
            }
        });
    }

    /// <summary>
    /// v1.2: Calculate and return Wealth Health snapshot
    /// </summary>
    [HttpPost("wealth-health/calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateWealthHealth([FromServices] LifeOS.Application.Services.IWealthHealthService wealthHealthService)
    {
        var userId = GetUserId();
        var snapshot = await wealthHealthService.CalculateWealthHealthAsync(userId);
        
        return Ok(new
        {
            data = new
            {
                score = snapshot.Score,
                components = System.Text.Json.JsonSerializer.Deserialize<object>(snapshot.Components),
                timestamp = snapshot.Timestamp
            }
        });
    }

    /// <summary>
    /// v1.2: Calculate and return comprehensive LifeOS Score snapshot
    /// Aggregates Health Index, Adherence, Wealth Health, and Longevity
    /// </summary>
    [HttpPost("lifeos-score/calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateLifeOsScore([FromServices] LifeOS.Application.Services.ILifeOsScoreService lifeOsScoreService)
    {
        var userId = GetUserId();
        var snapshot = await lifeOsScoreService.CalculateLifeOsScoreAsync(userId);
        
        return Ok(new
        {
            data = new
            {
                lifeScore = snapshot.LifeScore,
                healthIndex = snapshot.HealthIndex,
                adherenceIndex = snapshot.AdherenceIndex,
                wealthHealthScore = snapshot.WealthHealthScore,
                longevityYearsAdded = snapshot.LongevityYearsAdded,
                dimensionScores = System.Text.Json.JsonSerializer.Deserialize<object>(snapshot.DimensionScores),
                timestamp = snapshot.Timestamp
            }
        });
    }

    /// <summary>
    /// v1.2: Get latest snapshots for all scoring systems
    /// </summary>
    [HttpGet("snapshots/latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestSnapshots()
    {
        var userId = GetUserId();

        var healthIndex = await _context.HealthIndexSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync();

        var adherence = await _context.AdherenceSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync();

        var wealthHealth = await _context.WealthHealthSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync();

        var lifeOsScore = await _context.LifeOsScoreSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            data = new
            {
                healthIndex = healthIndex != null ? new
                {
                    score = healthIndex.Score,
                    timestamp = healthIndex.Timestamp,
                    components = System.Text.Json.JsonSerializer.Deserialize<object>(healthIndex.Components)
                } : null,
                adherence = adherence != null ? new
                {
                    score = adherence.Score,
                    timeWindowDays = adherence.TimeWindowDays,
                    tasksConsidered = adherence.TasksConsidered,
                    tasksCompleted = adherence.TasksCompleted,
                    timestamp = adherence.Timestamp
                } : null,
                wealthHealth = wealthHealth != null ? new
                {
                    score = wealthHealth.Score,
                    timestamp = wealthHealth.Timestamp,
                    components = System.Text.Json.JsonSerializer.Deserialize<object>(wealthHealth.Components)
                } : null,
                lifeOsScore = lifeOsScore != null ? new
                {
                    lifeScore = lifeOsScore.LifeScore,
                    healthIndex = lifeOsScore.HealthIndex,
                    adherenceIndex = lifeOsScore.AdherenceIndex,
                    wealthHealthScore = lifeOsScore.WealthHealthScore,
                    longevityYearsAdded = lifeOsScore.LongevityYearsAdded,
                    dimensionScores = System.Text.Json.JsonSerializer.Deserialize<object>(lifeOsScore.DimensionScores),
                    timestamp = lifeOsScore.Timestamp
                } : null
            }
        });
    }

    #endregion
}
