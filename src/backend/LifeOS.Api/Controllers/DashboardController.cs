using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Services;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ILifeOSDbContext _context;
    private readonly IScoreCalculator _scoreCalculator;
    private readonly IWealthHealthScoreService _wealthHealthService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ILifeOSDbContext context, 
        IScoreCalculator scoreCalculator,
        IWealthHealthScoreService wealthHealthService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _scoreCalculator = scoreCalculator;
        _wealthHealthService = wealthHealthService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get complete dashboard data
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserId();
        
        // Calculate actual life score using ScoreCalculator
        var lifeScore = await _scoreCalculator.CalculateLifeScoreAsync(userId);
        
        // Get dimensions with calculated scores
        var dimensionList = await _context.Dimensions
            .Where(d => d.IsActive)
            .ToListAsync();
        
        var dimensions = new List<object>();
        foreach (var d in dimensionList)
        {
            var score = await _scoreCalculator.CalculateDimensionScoreAsync(userId, d.Id);
            var trend = await CalculateTrendAsync(userId, d.Code);
            
            dimensions.Add(new {
                id = d.Id,
                code = d.Code,
                name = d.Name,
                icon = d.Icon,
                score = score,
                trend = trend,
                activeMilestones = await _context.Milestones
                    .CountAsync(m => m.DimensionId == d.Id && m.Status == MilestoneStatus.Active)
            });
        }

        // Get accounts for net worth
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsLiability).Sum(a => a.CurrentBalance);
        var netWorth = totalAssets - totalLiabilities;

        // Get streaks
        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.CurrentStreakLength)
            .Take(5)
            .Select(s => new {
                id = s.Id,
                name = s.TaskId != null 
                    ? _context.Tasks.Where(t => t.Id == s.TaskId).Select(t => t.Title).FirstOrDefault()
                    : s.MetricCode,
                habitId = s.TaskId,
                currentDays = s.CurrentStreakLength,
                longestDays = s.LongestStreakLength,
                lastCompletedAt = s.LastSuccessDate
            })
            .ToListAsync();

        // Get today's tasks
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.IsActive && 
                   (t.ScheduledDate == null || t.ScheduledDate == today))
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.Title)
            .Take(10)
            .Select(t => new {
                id = t.Id,
                title = t.Title,
                completed = t.IsCompleted,
                dimensionId = t.Dimension != null ? t.Dimension.Code : null
            })
            .ToListAsync();

        // Calculate life score trend
        var lifeScoreTrend = await CalculateLifeScoreTrendAsync(userId);

        return Ok(new {
            data = new {
                lifeScore,
                lifeScoreTrend,
                netWorth = new {
                    value = netWorth,
                    totalAssets,
                    totalLiabilities,
                    currency = "ZAR",
                    change = 0m,
                    changePercent = 0m
                },
                dimensions,
                streaks,
                tasks
            },
            meta = new {
                timestamp = DateTime.UtcNow
            }
        });
    }
    
    /// <summary>
    /// Calculate trend as percentage change between current and previous 7-day periods
    /// </summary>
    private async Task<decimal> CalculateTrendAsync(Guid userId, string dimensionCode)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = today.AddDays(-7);
        var fourteenDaysAgo = today.AddDays(-14);
        
        var scoreCode = $"{dimensionCode}_score";
        
        // Get current period average
        var currentScores = await _context.ScoreRecords
            .Where(s => s.UserId == userId && s.ScoreCode == scoreCode 
                && s.PeriodStart >= sevenDaysAgo && s.PeriodStart <= today)
            .Select(s => s.ScoreValue)
            .ToListAsync();
        
        // Get previous period average
        var previousScores = await _context.ScoreRecords
            .Where(s => s.UserId == userId && s.ScoreCode == scoreCode 
                && s.PeriodStart >= fourteenDaysAgo && s.PeriodStart < sevenDaysAgo)
            .Select(s => s.ScoreValue)
            .ToListAsync();
        
        if (!currentScores.Any() || !previousScores.Any())
            return 0;
        
        var currentAvg = currentScores.Average();
        var previousAvg = previousScores.Average();
        
        if (previousAvg == 0)
            return 0;
        
        return Math.Round(((currentAvg - previousAvg) / previousAvg) * 100, 1);
    }
    
    private async Task<decimal> CalculateLifeScoreTrendAsync(Guid userId)
    {
        return await CalculateTrendAsync(userId, "life");
    }

    /// <summary>
    /// Get net worth data
    /// </summary>
    [HttpGet("net-worth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetWorth([FromQuery] string currency = "ZAR")
    {
        var userId = GetUserId();
        
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsLiability).Sum(a => a.CurrentBalance);
        var netWorth = totalAssets - totalLiabilities;

        // Get historical data from net worth projections or account snapshots
        // For now, return just current value
        return Ok(new {
            data = new {
                value = netWorth,
                totalAssets,
                totalLiabilities,
                currency,
                change = 0m,
                changePercent = 0m,
                history = new List<object>() // Would be populated from historical data
            },
            meta = new {
                timestamp = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Get historical net worth data
    /// </summary>
    [HttpGet("net-worth/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetWorthHistory(
        [FromQuery] string period = "1Y",
        [FromQuery] string currency = "ZAR")
    {
        var userId = GetUserId();
        
        // Calculate date range based on period
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = period switch
        {
            "1M" => today.AddMonths(-1),
            "3M" => today.AddMonths(-3),
            "6M" => today.AddMonths(-6),
            "1Y" => today.AddYears(-1),
            "ALL" => today.AddYears(-10),
            _ => today.AddYears(-1)
        };
        
        var snapshots = await _context.NetWorthSnapshots
            .Where(s => s.UserId == userId && s.SnapshotDate >= fromDate)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new {
                date = s.SnapshotDate.ToString("yyyy-MM-dd"),
                value = s.NetWorth,
                totalAssets = s.TotalAssets,
                totalLiabilities = s.TotalLiabilities
            })
            .ToListAsync();
        
        // Calculate change metrics
        decimal change = 0;
        decimal changePercent = 0;
        
        if (snapshots.Count >= 2)
        {
            var first = snapshots.First();
            var last = snapshots.Last();
            change = last.value - first.value;
            changePercent = first.value != 0 
                ? Math.Round((change / first.value) * 100, 2) 
                : 0;
        }
        
        return Ok(new {
            data = new {
                history = snapshots,
                summary = new {
                    currentNetWorth = snapshots.LastOrDefault()?.value ?? 0,
                    change,
                    changePercent,
                    period,
                    dataPoints = snapshots.Count
                }
            },
            meta = new {
                timestamp = DateTime.UtcNow,
                currency
            }
        });
    }

    /// <summary>
    /// Get dimension scores
    /// </summary>
    [HttpGet("dimensions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDimensionScores()
    {
        var userId = GetUserId();
        
        var dimensionList = await _context.Dimensions
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync();
        
        var dimensions = new List<object>();
        foreach (var d in dimensionList)
        {
            var score = await _scoreCalculator.CalculateDimensionScoreAsync(userId, d.Id);
            var trend = await CalculateTrendAsync(userId, d.Code);
            
            dimensions.Add(new {
                id = d.Code,
                code = d.Code,
                name = d.Name,
                icon = d.Icon,
                score = score,
                trend = trend,
                activeMilestones = await _context.Milestones
                    .CountAsync(m => m.DimensionId == d.Id && m.Status == MilestoneStatus.Active)
            });
        }

        return Ok(new { data = dimensions });
    }

    /// <summary>
    /// Get active streaks
    /// </summary>
    [HttpGet("streaks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStreaks()
    {
        var userId = GetUserId();
        
        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId && s.IsActive && s.CurrentStreakLength > 0)
            .OrderByDescending(s => s.CurrentStreakLength)
            .Take(10)
            .Select(s => new {
                id = s.Id,
                name = s.TaskId != null 
                    ? _context.Tasks.Where(t => t.Id == s.TaskId).Select(t => t.Title).FirstOrDefault()
                    : s.MetricCode,
                habitId = s.TaskId,
                currentDays = s.CurrentStreakLength,
                longestDays = s.LongestStreakLength,
                lastCompletedAt = s.LastSuccessDate
            })
            .ToListAsync();

        return Ok(new { data = streaks });
    }

    /// <summary>
    /// Get today's tasks
    /// </summary>
    [HttpGet("tasks/today")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodaysTasks()
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.IsActive && 
                   (t.ScheduledDate == null || t.ScheduledDate == today))
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.Title)
            .Take(20)
            .Select(t => new {
                id = t.Id,
                title = t.Title,
                completed = t.IsCompleted,
                dimensionId = t.Dimension != null ? t.Dimension.Code : null
            })
            .ToListAsync();

        return Ok(new { data = tasks });
    }

    /// <summary>
    /// Get projections summary
    /// </summary>
    [HttpGet("projections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjections()
    {
        var userId = GetUserId();
        
        // Get net worth projections from baseline scenario
        var baseline = await _context.SimulationScenarios
            .Where(s => s.UserId == userId && s.IsBaseline)
            .FirstOrDefaultAsync();

        if (baseline == null)
        {
            return Ok(new { data = new List<object>() });
        }

        var projections = await _context.NetWorthProjections
            .Where(p => p.ScenarioId == baseline.Id)
            .OrderBy(p => p.PeriodDate)
            .Take(60) // 5 years
            .Select(p => new {
                period = p.PeriodDate.ToString("yyyy-MM"),
                netWorth = p.NetWorth,
                totalAssets = p.TotalAssets,
                totalLiabilities = p.TotalLiabilities
            })
            .ToListAsync();

        return Ok(new { data = projections });
    }

    /// <summary>
    /// Get wealth health score
    /// </summary>
    [HttpGet("wealth-health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWealthHealth()
    {
        var userId = GetUserId();
        var result = await _wealthHealthService.CalculateAsync(userId);
        
        return Ok(new {
            data = new {
                overallScore = result.OverallScore,
                components = new {
                    savingsRate = result.SavingsRateScore,
                    debtToIncome = result.DebtToIncomeScore,
                    emergencyFund = result.EmergencyFundScore,
                    diversification = result.DiversificationScore,
                    netWorthGrowth = result.NetWorthGrowthScore
                },
                details = result.Details
            },
            meta = new {
                timestamp = DateTime.UtcNow
            }
        });
    }
}
