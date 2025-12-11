using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<ReviewsController> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ReviewsController(
        ILifeOSDbContext context, 
        ILogger<ReviewsController> logger,
        IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get the current week's review
    /// </summary>
    [HttpGet("weekly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWeeklyReview()
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        // Get most recently generated review for this week that has current values
        var review = await _context.ReviewSnapshots
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.ReviewType == "weekly" && r.PeriodStart >= weekStart)
            .Where(r => r.HealthIndexCurrent != null) // Only reviews with v1.1 calculated values
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync();

        if (review == null)
        {
            // Generate on-the-fly if no v1.1 review exists
            review = await GenerateWeeklyReviewAsync(userId, weekStart, today);
        }

        return Ok(new { data = FormatReviewResponse(review) });
    }

    /// <summary>
    /// Get the current month's review
    /// </summary>
    [HttpGet("monthly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMonthlyReview()
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var review = await _context.ReviewSnapshots
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.ReviewType == "monthly" && r.PeriodStart >= monthStart)
            .Where(r => r.HealthIndexCurrent != null) // Only reviews with v1.1 calculated values
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync();

        if (review == null)
        {
            review = await GenerateMonthlyReviewAsync(userId, monthStart, today);
        }

        return Ok(new { data = FormatReviewResponse(review) });
    }

    /// <summary>
    /// Get review history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewHistory([FromQuery] string type = "weekly", [FromQuery] int count = 10)
    {
        var userId = GetUserId();

        var reviews = await _context.ReviewSnapshots
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.ReviewType == type)
            .OrderByDescending(r => r.PeriodEnd)
            .Take(count)
            .ToListAsync();

        return Ok(new { data = reviews.Select(FormatReviewResponse) });
    }

    /// <summary>
    /// Force generate a review
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateReview([FromQuery] string type = "weekly")
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Notify via WebSocket that generation started
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReviewGenerationStarted", new { type, startedAt = DateTime.UtcNow });

        ReviewSnapshot review;
        if (type == "monthly")
        {
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            review = await GenerateMonthlyReviewAsync(userId, monthStart, today);
        }
        else
        {
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            review = await GenerateWeeklyReviewAsync(userId, weekStart, today);
        }

        _context.ReviewSnapshots.Add(review);
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Type} review generated for user {UserId}", type, userId);

        // Notify via WebSocket that generation completed
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReviewGenerationCompleted", new { type, completedAt = DateTime.UtcNow });

        return Ok(new { data = FormatReviewResponse(review) });
    }

    /// <summary>
    /// Get dimension-specific review
    /// </summary>
    [HttpGet("dimension/{dimensionCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDimensionReview(string dimensionCode, [FromQuery] string period = "weekly")
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var review = await GenerateDimensionReviewAsync(userId, dimensionCode, period, today);
        
        return Ok(new { data = review });
    }

    /// <summary>
    /// Get financial review with projections
    /// </summary>
    [HttpGet("financial")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinancialReview([FromQuery] string period = "monthly")
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var review = await GenerateFinancialReviewAsync(userId, period, today);
        
        return Ok(new { data = review });
    }

    private async Task<object> GenerateDimensionReviewAsync(Guid userId, string dimensionCode, string period, DateOnly today)
    {
        var dimension = await _context.Dimensions
            .FirstOrDefaultAsync(d => d.Code == dimensionCode);

        if (dimension == null)
            return new { error = "Dimension not found" };

        // Get metrics for this dimension
        var metrics = await _context.MetricDefinitions
            .Where(m => m.DimensionId == dimension.Id)
            .ToListAsync();

        var periodStart = period == "monthly" 
            ? new DateOnly(today.Year, today.Month, 1)
            : today.AddDays(-(int)today.DayOfWeek);

        var previousPeriodStart = period == "monthly"
            ? periodStart.AddMonths(-1)
            : periodStart.AddDays(-7);

        // Get metric records for current period
        var currentRecords = await _context.MetricRecords
            .Where(r => r.UserId == userId && 
                       metrics.Select(m => m.Code).Contains(r.MetricCode) &&
                       DateOnly.FromDateTime(r.RecordedAt) >= periodStart)
            .ToListAsync();

        // Get metric records for previous period
        var previousRecords = await _context.MetricRecords
            .Where(r => r.UserId == userId && 
                       metrics.Select(m => m.Code).Contains(r.MetricCode) &&
                       DateOnly.FromDateTime(r.RecordedAt) >= previousPeriodStart &&
                       DateOnly.FromDateTime(r.RecordedAt) < periodStart)
            .ToListAsync();

        // Calculate dimension score
        var dimensionScore = await CalculateDimensionScoreAsync(userId, dimension.Id);

        // Get tasks and streaks for this dimension
        var dimensionMilestones = await _context.Milestones
            .Where(m => m.UserId == userId && m.DimensionId == dimension.Id)
            .ToListAsync();

        var milestoneIds = dimensionMilestones.Select(m => m.Id).ToList();
        var dimensionTasks = await _context.Tasks
            .Where(t => t.UserId == userId && milestoneIds.Contains(t.MilestoneId ?? Guid.Empty))
            .ToListAsync();

        var taskIds = dimensionTasks.Select(t => t.Id).ToList();
        var dimensionStreaks = await _context.Streaks
            .Include(s => s.Task)
            .Where(s => s.UserId == userId && taskIds.Contains(s.TaskId ?? Guid.Empty))
            .ToListAsync();

        // Generate dimension-specific metrics summary
        var metricsSummary = metrics.Select(m => {
            var currentValues = currentRecords.Where(r => r.MetricCode == m.Code).ToList();
            var previousValues = previousRecords.Where(r => r.MetricCode == m.Code).ToList();
            
            var currentAvg = currentValues.Any() ? (decimal?)currentValues.Average(r => r.ValueNumber ?? 0) : null;
            var previousAvg = previousValues.Any() ? (decimal?)previousValues.Average(r => r.ValueNumber ?? 0) : null;
            
            return new {
                code = m.Code,
                name = m.Name,
                unit = m.Unit,
                currentValue = currentAvg,
                previousValue = previousAvg,
                delta = currentAvg.HasValue && previousAvg.HasValue ? currentAvg - previousAvg : null,
                recordCount = currentValues.Count
            };
        }).ToList();

        // Generate recommended actions for this dimension
        var actions = GenerateDimensionActions(dimensionCode, dimensionScore, metricsSummary);

        return new {
            dimension = new {
                code = dimension.Code,
                name = dimension.Name,
                score = dimensionScore
            },
            periodStart = periodStart.ToString("yyyy-MM-dd"),
            periodEnd = today.ToString("yyyy-MM-dd"),
            period,
            metrics = metricsSummary,
            streaks = dimensionStreaks.Select(s => new {
                taskTitle = s.Task?.Title ?? "Unknown",
                currentStreak = s.CurrentStreakLength,
                longestStreak = s.LongestStreakLength,
                isActive = s.IsActive
            }),
            activeMilestones = dimensionMilestones.Where(m => m.Status != Domain.Enums.MilestoneStatus.Completed).Count(),
            completedMilestones = dimensionMilestones.Where(m => m.Status == Domain.Enums.MilestoneStatus.Completed).Count(),
            recommendedActions = actions,
            generatedAt = DateTime.UtcNow
        };
    }

    private async Task<object> GenerateFinancialReviewAsync(Guid userId, string period, DateOnly today)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        var periodStart = period == "monthly" 
            ? new DateOnly(today.Year, today.Month, 1)
            : today.AddDays(-(int)today.DayOfWeek);

        // Current net worth
        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsLiability).Sum(a => Math.Abs(a.CurrentBalance));
        var netWorth = totalAssets - totalLiabilities;

        // Get transactions in period
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.TransactionDate >= periodStart)
            .ToListAsync();

        var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var totalExpenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
        var netCashFlow = totalIncome - totalExpenses;

        // Get projections for comparison
        var baselineScenario = await _context.SimulationScenarios
            .Where(s => s.UserId == userId && s.IsBaseline)
            .FirstOrDefaultAsync();

        var projections = baselineScenario != null 
            ? await _context.NetWorthProjections
                .Where(p => p.ScenarioId == baselineScenario.Id)
                .OrderBy(p => p.PeriodDate)
                .Take(12)
                .ToListAsync()
            : new List<NetWorthProjection>();

        // Calculate wealth health score
        var wealthHealth = await CalculateWealthHealthAsync(userId);

        // Account breakdown by type
        var accountBreakdown = accounts
            .GroupBy(a => a.AccountType)
            .Select(g => new {
                type = g.Key.ToString(),
                totalBalance = g.Sum(a => a.CurrentBalance),
                count = g.Count()
            }).ToList();

        // Generate financial recommendations
        var actions = GenerateFinancialActions(netWorth, wealthHealth, netCashFlow, totalLiabilities);

        // 12-month projection summary
        var projectionSummary = projections.Any() ? new {
            projectedNetWorthIn12Months = projections.LastOrDefault()?.NetWorth ?? netWorth,
            projectedGrowth = projections.Any() ? (projections.Last().NetWorth - netWorth) : 0,
            monthlyData = projections.Select(p => new {
                month = p.PeriodDate.ToString("yyyy-MM"),
                netWorth = p.NetWorth
            })
        } : null;

        return new {
            periodStart = periodStart.ToString("yyyy-MM-dd"),
            periodEnd = today.ToString("yyyy-MM-dd"),
            period,
            summary = new {
                netWorth,
                totalAssets,
                totalLiabilities,
                wealthHealthScore = wealthHealth,
                netCashFlow,
                totalIncome,
                totalExpenses,
                savingsRate = totalIncome > 0 ? Math.Round((totalIncome - totalExpenses) / totalIncome * 100, 1) : 0
            },
            accountBreakdown,
            projections = projectionSummary,
            recommendedActions = actions,
            generatedAt = DateTime.UtcNow
        };
    }

    private List<object> GenerateDimensionActions(string dimensionCode, decimal score, object metricsSummary)
    {
        var actions = new List<object>();

        switch (dimensionCode)
        {
            case "health_recovery":
                if (score < 60)
                    actions.Add(new { action = "Prioritize sleep - aim for 7-8 hours consistently", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Increase daily step count by 1000 steps", priority = "medium", dimension = dimensionCode });
                if (score < 80)
                    actions.Add(new { action = "Schedule a health checkup", priority = "low", dimension = dimensionCode });
                break;

            case "relationships":
                if (score < 60)
                    actions.Add(new { action = "Schedule quality time with family this week", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Reach out to a friend you haven't spoken to recently", priority = "medium", dimension = dimensionCode });
                break;

            case "work_contribution":
                if (score < 60)
                    actions.Add(new { action = "Set clear daily work priorities", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Block focused work time on calendar", priority = "medium", dimension = dimensionCode });
                break;

            case "play_adventure":
                if (score < 60)
                    actions.Add(new { action = "Schedule a fun activity this weekend", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Try something new or adventurous this month", priority = "medium", dimension = dimensionCode });
                break;

            case "asset_care":
                if (score < 60)
                    actions.Add(new { action = "Review and pay any overdue bills", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Check on home/car maintenance needs", priority = "medium", dimension = dimensionCode });
                break;

            case "create_craft":
                if (score < 60)
                    actions.Add(new { action = "Dedicate 30 minutes to a creative project", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Learn a new skill or technique", priority = "medium", dimension = dimensionCode });
                break;

            case "growth_mind":
                if (score < 60)
                    actions.Add(new { action = "Read or listen to educational content daily", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Practice meditation or mindfulness", priority = "medium", dimension = dimensionCode });
                break;

            case "community_meaning":
                if (score < 60)
                    actions.Add(new { action = "Volunteer or help someone in need", priority = "high", dimension = dimensionCode });
                if (score < 70)
                    actions.Add(new { action = "Engage with your community or a cause you care about", priority = "medium", dimension = dimensionCode });
                break;
        }

        if (!actions.Any())
        {
            actions.Add(new { action = $"Maintain excellent progress in {dimensionCode.Replace("_", " ")}", priority = "low", dimension = dimensionCode });
        }

        return actions;
    }

    private List<object> GenerateFinancialActions(decimal netWorth, decimal wealthHealth, decimal netCashFlow, decimal totalLiabilities)
    {
        var actions = new List<object>();

        if (netWorth < 0)
            actions.Add(new { action = "Focus on paying down high-interest debt first", priority = "high", dimension = "finances" });

        if (wealthHealth < 40)
            actions.Add(new { action = "Build emergency fund to cover 3 months expenses", priority = "high", dimension = "finances" });

        if (netCashFlow < 0)
            actions.Add(new { action = "Review and reduce discretionary spending", priority = "high", dimension = "finances" });

        if (totalLiabilities > netWorth * 0.5m)
            actions.Add(new { action = "Create a debt payoff strategy (avalanche or snowball)", priority = "medium", dimension = "finances" });

        if (wealthHealth >= 40 && wealthHealth < 70)
            actions.Add(new { action = "Increase investment contributions by 1%", priority = "medium", dimension = "finances" });

        if (wealthHealth >= 70)
            actions.Add(new { action = "Review investment allocation and rebalance if needed", priority = "low", dimension = "finances" });

        if (!actions.Any())
        {
            actions.Add(new { action = "Maintain excellent financial health - consider increasing savings rate", priority = "low", dimension = "finances" });
        }

        return actions;
    }

    private async Task<decimal> CalculateDimensionScoreAsync(Guid userId, Guid dimensionId)
    {
        // Get dimension score from stored calculation or calculate on-the-fly
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        var dimensionMetrics = await _context.MetricDefinitions
            .Where(m => m.DimensionId == dimensionId)
            .Select(m => m.Code)
            .ToListAsync();

        if (!dimensionMetrics.Any())
            return 50m;

        var recentRecords = await _context.MetricRecords
            .Where(r => r.UserId == userId && 
                       dimensionMetrics.Contains(r.MetricCode) &&
                       r.RecordedAt >= thirtyDaysAgo)
            .ToListAsync();

        if (!recentRecords.Any())
            return 50m;

        // Simple scoring: normalize values based on target ranges
        // This is a simplified version - in production would use MetricDefinition targets
        return Math.Round(50m + (recentRecords.Count * 2m), 1);
    }

    private async Task<ReviewSnapshot> GenerateWeeklyReviewAsync(Guid userId, DateOnly periodStart, DateOnly periodEnd)
    {
        // Get top streaks
        var topStreaks = await _context.Streaks
            .Include(s => s.Task)
            .Where(s => s.UserId == userId && s.IsActive && s.CurrentStreakLength > 0)
            .OrderByDescending(s => s.CurrentStreakLength)
            .Take(5)
            .Select(s => new { taskId = s.TaskId, taskTitle = s.Task != null ? s.Task.Title : "Unknown", streakDays = s.CurrentStreakLength })
            .ToListAsync();

        // Calculate Health Index
        var healthIndex = await CalculateHealthIndexAsync(userId);
        
        // Calculate Adherence Index
        var adherenceIndex = await CalculateAdherenceIndexAsync(userId);
        
        // Calculate Wealth Health
        var wealthHealth = await CalculateWealthHealthAsync(userId);
        
        // Calculate Longevity
        var longevity = await CalculateLongevityAsync(userId);

        // Get previous week's data for delta calculation
        var previousWeekStart = periodStart.AddDays(-7);
        var previousReview = await _context.ReviewSnapshots
            .Where(r => r.UserId == userId && r.ReviewType == "weekly" && r.PeriodStart == previousWeekStart)
            .FirstOrDefaultAsync();

        var healthDelta = previousReview != null ? healthIndex - previousReview.HealthIndexCurrent : 0;
        var adherenceDelta = previousReview != null ? adherenceIndex - previousReview.AdherenceIndexCurrent : 0;
        var wealthDelta = previousReview != null ? wealthHealth - previousReview.WealthHealthCurrent : 0;
        var longevityDelta = previousReview != null ? longevity - previousReview.LongevityCurrent : 0;

        // Generate recommended actions based on scores
        var recommendedActions = await GenerateRecommendedActionsAsync(userId, healthIndex, adherenceIndex, wealthHealth);

        // Get current primary stats
        var primaryStats = await GetCurrentPrimaryStatsAsync(userId);
        var previousStats = previousReview != null && !string.IsNullOrEmpty(previousReview.PrimaryStatsDelta)
            ? JsonSerializer.Deserialize<Dictionary<string, decimal>>(previousReview.PrimaryStatsCurrent ?? "{}")
            : null;
        
        var statDeltas = new Dictionary<string, decimal>();
        foreach (var stat in primaryStats)
        {
            var delta = previousStats != null && previousStats.ContainsKey(stat.Key) 
                ? stat.Value - previousStats[stat.Key] 
                : 0;
            statDeltas[stat.Key] = delta;
        }

        // Calculate financial summary
        var financialSummary = await CalculateFinancialSummaryAsync(userId, periodStart, periodEnd);
        
        // Get dimension scores
        var dimensionScores = await GetAllDimensionScoresAsync(userId);

        return new ReviewSnapshot
        {
            UserId = userId,
            ReviewType = "weekly",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            HealthIndexCurrent = healthIndex,
            AdherenceIndexCurrent = adherenceIndex,
            WealthHealthCurrent = wealthHealth,
            LongevityCurrent = longevity,
            HealthIndexDelta = healthDelta,
            AdherenceIndexDelta = adherenceDelta,
            WealthHealthDelta = wealthDelta,
            LongevityDelta = longevityDelta,
            TopStreaks = JsonSerializer.Serialize(topStreaks),
            RecommendedActions = JsonSerializer.Serialize(recommendedActions),
            PrimaryStatsCurrent = JsonSerializer.Serialize(primaryStats),
            PrimaryStatsDelta = JsonSerializer.Serialize(statDeltas),
            NetWorthCurrent = financialSummary.NetWorth,
            NetWorthDelta = financialSummary.NetWorthDelta,
            TotalIncome = financialSummary.TotalIncome,
            TotalExpenses = financialSummary.TotalExpenses,
            NetCashFlow = financialSummary.NetCashFlow,
            SavingsRate = financialSummary.SavingsRate,
            DimensionScores = JsonSerializer.Serialize(dimensionScores),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<(decimal NetWorth, decimal NetWorthDelta, decimal TotalIncome, decimal TotalExpenses, decimal NetCashFlow, decimal SavingsRate)> CalculateFinancialSummaryAsync(Guid userId, DateOnly periodStart, DateOnly periodEnd)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsLiability).Sum(a => Math.Abs(a.CurrentBalance));
        var netWorth = totalAssets - totalLiabilities;

        // Get transactions in period
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && 
                       t.TransactionDate >= periodStart &&
                       t.TransactionDate <= periodEnd)
            .ToListAsync();

        var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var totalExpenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
        var netCashFlow = totalIncome - totalExpenses;
        var savingsRate = totalIncome > 0 ? Math.Round((totalIncome - totalExpenses) / totalIncome * 100, 1) : 0;

        // Calculate net worth delta (simplified - compare to previous period)
        var netWorthDelta = 0m; // Would need historical net worth tracking for accurate delta

        return (netWorth, netWorthDelta, totalIncome, totalExpenses, netCashFlow, savingsRate);
    }

    private async Task<Dictionary<string, decimal>> GetAllDimensionScoresAsync(Guid userId)
    {
        var dimensions = await _context.Dimensions.ToListAsync();
        var scores = new Dictionary<string, decimal>();
        
        foreach (var dimension in dimensions)
        {
            var score = await CalculateDimensionScoreAsync(userId, dimension.Id);
            scores[dimension.Code] = score;
        }
        
        return scores;
    }
    
    private async Task<decimal> CalculateHealthIndexAsync(Guid userId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var healthMetrics = await _context.MetricRecords
            .Where(m => m.UserId == userId && m.RecordedAt >= thirtyDaysAgo)
            .Where(m => m.MetricCode == "weight_kg" || m.MetricCode == "body_fat_pct" || 
                       m.MetricCode == "sleep_hours" || m.MetricCode == "steps_count")
            .OrderByDescending(m => m.RecordedAt)
            .ToListAsync();

        var latestWeight = healthMetrics.FirstOrDefault(m => m.MetricCode == "weight_kg")?.ValueNumber;
        var latestBodyFat = healthMetrics.FirstOrDefault(m => m.MetricCode == "body_fat_pct")?.ValueNumber;
        
        var sleepMetrics = healthMetrics.Where(m => m.MetricCode == "sleep_hours").Take(7).ToList();
        var avgSleep = sleepMetrics.Any() ? sleepMetrics.Average(m => m.ValueNumber ?? 0) : 7m;
        
        var stepsMetrics = healthMetrics.Where(m => m.MetricCode == "steps_count").Take(7).ToList();
        var avgSteps = stepsMetrics.Any() ? stepsMetrics.Average(m => m.ValueNumber ?? 0) : 5000m;

        // BMI Score (assuming 180cm height)
        var bmiScore = 50m;
        if (latestWeight.HasValue)
        {
            var heightM = 1.80m;
            var bmi = latestWeight.Value / (heightM * heightM);
            bmiScore = bmi >= 18.5m && bmi <= 24.9m ? 100m : 
                      bmi >= 25m && bmi <= 29.9m ? 70m : 
                      bmi >= 30m ? 40m : 60m;
        }

        // Body Fat Score
        var bodyFatScore = 50m;
        if (latestBodyFat.HasValue)
        {
            bodyFatScore = latestBodyFat.Value <= 15m ? 100m :
                          latestBodyFat.Value <= 20m ? 85m :
                          latestBodyFat.Value <= 25m ? 70m :
                          latestBodyFat.Value <= 30m ? 50m : 30m;
        }

        // Sleep Score
        var sleepScore = avgSleep >= 7m && avgSleep <= 9m ? 100m :
                        avgSleep >= 6m ? 70m : 40m;

        // Activity Score (based on steps)
        var activityScore = avgSteps >= 10000m ? 100m :
                           avgSteps >= 7500m ? 80m :
                           avgSteps >= 5000m ? 60m :
                           avgSteps >= 2500m ? 40m : 20m;

        return Math.Round((bmiScore * 0.25m) + (bodyFatScore * 0.25m) + (sleepScore * 0.25m) + (activityScore * 0.25m), 1);
    }

    private async Task<decimal> CalculateAdherenceIndexAsync(Guid userId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync();

        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var avgStreakLength = streaks.Any() ? (decimal)streaks.Average(s => s.CurrentStreakLength) : 0m;
        var missedPenaltyTotal = streaks.Sum(s => s.RiskPenaltyScore);

        var streakScore = Math.Min(100m, avgStreakLength * 10m);
        var penaltyScore = Math.Max(0m, 100m - missedPenaltyTotal);
        
        return Math.Round((streakScore * 0.6m) + (penaltyScore * 0.4m), 1);
    }

    private async Task<decimal> CalculateWealthHealthAsync(Guid userId)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        if (!accounts.Any()) return 50m;

        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsLiability).Sum(a => Math.Abs(a.CurrentBalance));

        var debtRatio = totalAssets > 0 ? (totalLiabilities / totalAssets) * 100 : 100;
        var debtScore = Math.Max(0, 100 - debtRatio);

        var emergencyFund = accounts
            .Where(a => a.AccountType == Domain.Enums.AccountType.Bank && !a.IsLiability)
            .Sum(a => a.CurrentBalance);
        var monthlyExpenses = 50000m;
        var emergencyMonths = monthlyExpenses > 0 ? emergencyFund / monthlyExpenses : 0;
        var emergencyScore = Math.Min(100m, emergencyMonths * 16.67m);

        var investmentRatio = totalAssets > 0 
            ? (accounts.Where(a => a.AccountType == Domain.Enums.AccountType.Investment)
                .Sum(a => a.CurrentBalance) / totalAssets) * 100 
            : 0;
        var investmentScore = Math.Min(100m, investmentRatio * 2m);

        return Math.Round((debtScore * 0.4m) + (emergencyScore * 0.3m) + (investmentScore * 0.3m), 1);
    }

    private async Task<decimal> CalculateLongevityAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        var baseLifeExpectancy = user?.LifeExpectancyBaseline ?? 80m;

        var longevityModels = await _context.LongevityModels
            .Where(m => m.IsActive)
            .ToListAsync();

        decimal totalYearsAdded = 0;
        foreach (var model in longevityModels)
        {
            totalYearsAdded += 1m; // Each active model adds ~1 year at baseline
        }

        totalYearsAdded = Math.Max(-20, Math.Min(20, totalYearsAdded));
        return Math.Round(baseLifeExpectancy + totalYearsAdded, 1);
    }

    private async Task<List<object>> GenerateRecommendedActionsAsync(Guid userId, decimal healthIndex, decimal adherenceIndex, decimal wealthHealth)
    {
        var actions = new List<object>();

        // Health-based recommendations
        if (healthIndex < 50)
            actions.Add(new { action = "Critical: Focus on improving sleep and daily activity immediately", priority = "high", dimension = "health_recovery" });
        else if (healthIndex < 70)
            actions.Add(new { action = "Improve your sleep quality and increase daily movement", priority = "medium", dimension = "health_recovery" });

        // Adherence-based recommendations
        if (adherenceIndex < 30)
            actions.Add(new { action = "Start with one small habit and build from there", priority = "high", dimension = "growth_mind" });
        else if (adherenceIndex < 50)
            actions.Add(new { action = "Rebuild your daily habits - focus on consistency over intensity", priority = "high", dimension = "growth_mind" });
        else if (adherenceIndex < 70)
            actions.Add(new { action = "You're building momentum - try adding one more habit", priority = "medium", dimension = "growth_mind" });

        // Wealth-based recommendations
        if (wealthHealth < 30)
            actions.Add(new { action = "Urgent: Create a budget and track all expenses", priority = "high", dimension = "asset_care" });
        else if (wealthHealth < 50)
            actions.Add(new { action = "Review and optimize your debt management strategy", priority = "high", dimension = "asset_care" });
        else if (wealthHealth < 70)
            actions.Add(new { action = "Consider increasing your investment contributions", priority = "medium", dimension = "asset_care" });

        // Get dimension scores and add dimension-specific recommendations
        var dimensions = await _context.Dimensions.ToListAsync();
        foreach (var dimension in dimensions)
        {
            var score = await CalculateDimensionScoreAsync(userId, dimension.Id);
            if (score < 40 && dimension.Code != "health_recovery" && dimension.Code != "asset_care")
            {
                var actionText = dimension.Code switch
                {
                    "relationships" => "Schedule quality time with loved ones this week",
                    "work_contribution" => "Set clear priorities and protect focused work time",
                    "play_adventure" => "Plan a fun activity or adventure for this weekend",
                    "create_craft" => "Dedicate time to your creative projects",
                    "community_meaning" => "Connect with your community or a cause you care about",
                    _ => $"Focus on improving your {dimension.Name} score"
                };
                actions.Add(new { action = actionText, priority = "medium", dimension = dimension.Code });
            }
        }

        // Check for broken streaks
        var brokenStreaks = await _context.Streaks
            .Include(s => s.Task)
            .Where(s => s.UserId == userId && s.ConsecutiveMisses >= 2)
            .Take(3)
            .ToListAsync();

        foreach (var streak in brokenStreaks)
        {
            actions.Add(new { 
                action = $"Resume '{streak.Task?.Title ?? "habit"}' - {streak.ConsecutiveMisses} days missed", 
                priority = streak.ConsecutiveMisses >= 5 ? "high" : "medium", 
                dimension = "growth_mind" 
            });
        }

        // Check for upcoming milestone deadlines
        var upcomingMilestones = await _context.Milestones
            .Where(m => m.UserId == userId && m.Status != Domain.Enums.MilestoneStatus.Completed && 
                       m.TargetDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .Take(2)
            .ToListAsync();

        foreach (var milestone in upcomingMilestones)
        {
            actions.Add(new { 
                action = $"Deadline approaching: '{milestone.Title}' due {milestone.TargetDate:MMM dd}", 
                priority = "high", 
                dimension = "growth_mind" 
            });
        }

        // Limit to top 5 actions sorted by priority
        var priorityOrder = new Dictionary<string, int> { { "high", 0 }, { "medium", 1 }, { "low", 2 } };
        actions = actions
            .OrderBy(a => {
                var priority = ((dynamic)a).priority as string ?? "low";
                return priorityOrder.ContainsKey(priority) ? priorityOrder[priority] : 2;
            })
            .Take(5)
            .ToList();

        if (!actions.Any())
        {
            actions.Add(new { action = "Excellent progress! Maintain your current momentum", priority = "low", dimension = "general" });
        }

        return actions;
    }

    private async Task<Dictionary<string, decimal>> GetCurrentPrimaryStatsAsync(Guid userId)
    {
        var latestStats = await _context.PrimaryStatRecords
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.RecordedAt)
            .FirstOrDefaultAsync();

        if (latestStats != null)
        {
            return new Dictionary<string, decimal>
            {
                { "strength", latestStats.Strength },
                { "wisdom", latestStats.Wisdom },
                { "charisma", latestStats.Charisma },
                { "composure", latestStats.Composure },
                { "energy", latestStats.Energy },
                { "influence", latestStats.Influence },
                { "vitality", latestStats.Vitality }
            };
        }

        return new Dictionary<string, decimal>
        {
            { "strength", 50 },
            { "wisdom", 50 },
            { "charisma", 50 },
            { "composure", 50 },
            { "energy", 50 },
            { "influence", 50 },
            { "vitality", 50 }
        };
    }

    private async Task<ReviewSnapshot> GenerateMonthlyReviewAsync(Guid userId, DateOnly periodStart, DateOnly periodEnd)
    {
        // Get top streaks
        var topStreaks = await _context.Streaks
            .Include(s => s.Task)
            .Where(s => s.UserId == userId && s.IsActive && s.CurrentStreakLength > 0)
            .OrderByDescending(s => s.CurrentStreakLength)
            .Take(5)
            .Select(s => new { taskId = s.TaskId, taskTitle = s.Task != null ? s.Task.Title : "Unknown", streakDays = s.CurrentStreakLength })
            .ToListAsync();

        // Calculate current values
        var healthIndex = await CalculateHealthIndexAsync(userId);
        var adherenceIndex = await CalculateAdherenceIndexAsync(userId);
        var wealthHealth = await CalculateWealthHealthAsync(userId);
        var longevity = await CalculateLongevityAsync(userId);

        // Get previous month's data for delta calculation
        var previousMonthStart = periodStart.AddMonths(-1);
        var previousReview = await _context.ReviewSnapshots
            .Where(r => r.UserId == userId && r.ReviewType == "monthly" && r.PeriodStart == previousMonthStart)
            .FirstOrDefaultAsync();

        var healthDelta = previousReview != null ? healthIndex - previousReview.HealthIndexCurrent : 0;
        var adherenceDelta = previousReview != null ? adherenceIndex - previousReview.AdherenceIndexCurrent : 0;
        var wealthDelta = previousReview != null ? wealthHealth - previousReview.WealthHealthCurrent : 0;
        var longevityDelta = previousReview != null ? longevity - previousReview.LongevityCurrent : 0;

        var recommendedActions = await GenerateRecommendedActionsAsync(userId, healthIndex, adherenceIndex, wealthHealth);
        var primaryStats = await GetCurrentPrimaryStatsAsync(userId);
        
        // Calculate financial summary for monthly
        var financialSummary = await CalculateFinancialSummaryAsync(userId, periodStart, periodEnd);
        
        // Get dimension scores
        var dimensionScores = await GetAllDimensionScoresAsync(userId);

        return new ReviewSnapshot
        {
            UserId = userId,
            ReviewType = "monthly",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            HealthIndexCurrent = healthIndex,
            AdherenceIndexCurrent = adherenceIndex,
            WealthHealthCurrent = wealthHealth,
            LongevityCurrent = longevity,
            HealthIndexDelta = healthDelta,
            AdherenceIndexDelta = adherenceDelta,
            WealthHealthDelta = wealthDelta,
            LongevityDelta = longevityDelta,
            TopStreaks = JsonSerializer.Serialize(topStreaks),
            RecommendedActions = JsonSerializer.Serialize(recommendedActions),
            PrimaryStatsCurrent = JsonSerializer.Serialize(primaryStats),
            PrimaryStatsDelta = JsonSerializer.Serialize(new Dictionary<string, decimal>()),
            NetWorthCurrent = financialSummary.NetWorth,
            NetWorthDelta = financialSummary.NetWorthDelta,
            TotalIncome = financialSummary.TotalIncome,
            TotalExpenses = financialSummary.TotalExpenses,
            NetCashFlow = financialSummary.NetCashFlow,
            SavingsRate = financialSummary.SavingsRate,
            DimensionScores = JsonSerializer.Serialize(dimensionScores),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static object FormatReviewResponse(ReviewSnapshot review)
    {
        return new
        {
            periodStart = review.PeriodStart.ToString("yyyy-MM-dd"),
            periodEnd = review.PeriodEnd.ToString("yyyy-MM-dd"),
            healthIndex = review.HealthIndexCurrent,
            adherenceIndex = review.AdherenceIndexCurrent,
            wealthHealth = review.WealthHealthCurrent,
            longevity = review.LongevityCurrent,
            healthIndexDelta = review.HealthIndexDelta,
            adherenceIndexDelta = review.AdherenceIndexDelta,
            wealthHealthDelta = review.WealthHealthDelta,
            longevityDelta = review.LongevityDelta,
            topStreaks = string.IsNullOrEmpty(review.TopStreaks) 
                ? new List<object>() 
                : JsonSerializer.Deserialize<List<object>>(review.TopStreaks),
            recommendedActions = string.IsNullOrEmpty(review.RecommendedActions)
                ? new List<object>()
                : JsonSerializer.Deserialize<List<object>>(review.RecommendedActions),
            primaryStats = string.IsNullOrEmpty(review.PrimaryStatsCurrent)
                ? new Dictionary<string, decimal>()
                : JsonSerializer.Deserialize<Dictionary<string, decimal>>(review.PrimaryStatsCurrent),
            primaryStatsDelta = string.IsNullOrEmpty(review.PrimaryStatsDelta)
                ? new Dictionary<string, decimal>()
                : JsonSerializer.Deserialize<Dictionary<string, decimal>>(review.PrimaryStatsDelta),
            // Financial data
            financialSummary = new {
                netWorth = review.NetWorthCurrent,
                netWorthDelta = review.NetWorthDelta,
                totalIncome = review.TotalIncome,
                totalExpenses = review.TotalExpenses,
                netCashFlow = review.NetCashFlow,
                savingsRate = review.SavingsRate
            },
            dimensionScores = string.IsNullOrEmpty(review.DimensionScores)
                ? new Dictionary<string, decimal>()
                : JsonSerializer.Deserialize<Dictionary<string, decimal>>(review.DimensionScores),
            generatedAt = review.GeneratedAt
        };
    }
}
