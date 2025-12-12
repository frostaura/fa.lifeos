using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces.Mcp;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.getWeeklyReview
/// Retrieves weekly performance review with score changes, streaks, and focus actions.
/// </summary>
public class GetWeeklyReviewHandler : IMcpToolHandler
{
    private readonly ILifeOSDbContext _context;
    
    public string ToolName => "lifeos.getWeeklyReview";
    public string Description => "Get weekly performance review with score changes, top streaks, at-risk streaks, and focus actions";
    
    public GetWeeklyReviewHandler(ILifeOSDbContext context)
    {
        _context = context;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse input
            WeeklyReviewRequestDto? request = null;
            if (!string.IsNullOrEmpty(jsonInput))
            {
                request = JsonSerializer.Deserialize<WeeklyReviewRequestDto>(
                    jsonInput, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            // Determine week period
            var weekStart = request?.WeekStartDate ?? GetCurrentWeekStart();
            var weekEnd = weekStart.AddDays(6);
            
            // Get score snapshots for week start and end
            var weekStartSnapshot = await GetSnapshotNearDate(userId, weekStart, cancellationToken);
            var weekEndSnapshot = await GetSnapshotNearDate(userId, weekEnd, cancellationToken);
            
            // Get streaks
            var topStreaks = await GetTopStreaks(userId, cancellationToken);
            var atRiskStreaks = await GetAtRiskStreaks(userId, cancellationToken);
            
            // Generate focus actions based on score changes
            var focusActions = GenerateFocusActions(weekStartSnapshot, weekEndSnapshot);
            
            // Build response
            var review = new WeeklyReviewDto
            {
                Period = new PeriodDto
                {
                    Start = weekStart.ToString("yyyy-MM-dd"),
                    End = weekEnd.ToString("yyyy-MM-dd")
                },
                HealthIndexChange = weekStartSnapshot != null && weekEndSnapshot != null
                    ? new ScoreChangeDto 
                    { 
                        From = Math.Round(weekStartSnapshot.HealthIndex, 0), 
                        To = Math.Round(weekEndSnapshot.HealthIndex, 0) 
                    }
                    : null,
                AdherenceIndexChange = weekStartSnapshot != null && weekEndSnapshot != null
                    ? new ScoreChangeDto 
                    { 
                        From = Math.Round(weekStartSnapshot.AdherenceIndex, 0), 
                        To = Math.Round(weekEndSnapshot.AdherenceIndex, 0) 
                    }
                    : null,
                WealthHealthChange = weekStartSnapshot != null && weekEndSnapshot != null
                    ? new ScoreChangeDto 
                    { 
                        From = Math.Round(weekStartSnapshot.WealthHealthScore, 0), 
                        To = Math.Round(weekEndSnapshot.WealthHealthScore, 0) 
                    }
                    : null,
                LongevityChange = weekStartSnapshot != null && weekEndSnapshot != null
                    ? new LongevityChangeDto 
                    { 
                        From = Math.Round(weekStartSnapshot.LongevityYearsAdded, 1), 
                        To = Math.Round(weekEndSnapshot.LongevityYearsAdded, 1) 
                    }
                    : null,
                TopStreaks = topStreaks,
                AtRiskStreaks = atRiskStreaks,
                FocusActions = focusActions
            };
            
            return McpToolResponse<object>.Ok(review);
        }
        catch (JsonException ex)
        {
            return McpToolResponse<object>.Fail($"Invalid JSON input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to get weekly review: {ex.Message}");
        }
    }
    
    private DateTime GetCurrentWeekStart()
    {
        var today = DateTime.UtcNow.Date;
        var daysToSubtract = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-daysToSubtract);
    }
    
    private async Task<Domain.Entities.LifeOsScoreSnapshot?> GetSnapshotNearDate(
        Guid userId, 
        DateTime targetDate, 
        CancellationToken cancellationToken)
    {
        // Find snapshot within 2 days of target date
        var snapshot = await _context.LifeOsScoreSnapshots
            .Where(s => s.UserId == userId)
            .Where(s => s.Timestamp >= targetDate.AddDays(-1) && s.Timestamp <= targetDate.AddDays(1))
            .OrderBy(s => Math.Abs((s.Timestamp - targetDate).Ticks))
            .FirstOrDefaultAsync(cancellationToken);
        
        return snapshot;
    }
    
    private async Task<List<TopStreakDto>> GetTopStreaks(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // Get top 3 active streaks
        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId && s.IsActive && s.CurrentStreakLength > 0)
            .Include(s => s.Task)
            .ThenInclude(t => t!.Dimension)
            .OrderByDescending(s => s.CurrentStreakLength)
            .Take(3)
            .ToListAsync(cancellationToken);
        
        return streaks.Select(s => new TopStreakDto
        {
            TaskTitle = s.Task?.Title ?? "Unknown Task",
            StreakLength = s.CurrentStreakLength,
            DimensionCode = s.Task?.Dimension?.Code ?? "uncategorized"
        }).ToList();
    }
    
    private async Task<List<AtRiskStreakDto>> GetAtRiskStreaks(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // Get streaks with consecutive misses (potential risk)
        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId && s.IsActive && s.ConsecutiveMisses >= 2)
            .Include(s => s.Task)
            .OrderByDescending(s => s.ConsecutiveMisses)
            .Take(5)
            .ToListAsync(cancellationToken);
        
        return streaks.Select(s => new AtRiskStreakDto
        {
            TaskTitle = s.Task?.Title ?? "Unknown Task",
            ConsecutiveMisses = s.ConsecutiveMisses,
            RiskPenaltyScore = CalculateRiskPenalty(s.ConsecutiveMisses)
        }).ToList();
    }
    
    private int CalculateRiskPenalty(int consecutiveMisses)
    {
        // Match streak penalty logic: 0 on 1st miss, 5 on 2nd, 10×(n-1) after
        if (consecutiveMisses <= 1) return 0;
        if (consecutiveMisses == 2) return 5;
        return 10 * (consecutiveMisses - 1);
    }
    
    private List<string> GenerateFocusActions(
        Domain.Entities.LifeOsScoreSnapshot? startSnapshot,
        Domain.Entities.LifeOsScoreSnapshot? endSnapshot)
    {
        var actions = new List<string>();
        
        if (startSnapshot == null || endSnapshot == null)
        {
            // Default recommendations if no data
            actions.Add("Complete weekly review to establish baseline metrics.");
            actions.Add("Record daily metrics consistently for better insights.");
            return actions;
        }
        
        // Health Index recommendations
        if (endSnapshot.HealthIndex < startSnapshot.HealthIndex)
        {
            actions.Add("Health Index declined this week. Review health metrics and adjust routines.");
        }
        else if (endSnapshot.HealthIndex < 70)
        {
            actions.Add("Focus on improving health metrics. Consider adding cardio or sleep optimization.");
        }
        
        // Adherence recommendations
        if (endSnapshot.AdherenceIndex < startSnapshot.AdherenceIndex)
        {
            actions.Add("Task adherence dropped. Review at-risk streaks and re-commit to daily habits.");
        }
        else if (endSnapshot.AdherenceIndex < 75)
        {
            actions.Add("Strengthen habit streaks by completing critical daily tasks consistently.");
        }
        
        // Wealth recommendations
        if (endSnapshot.WealthHealthScore < startSnapshot.WealthHealthScore)
        {
            actions.Add("Wealth health declined. Review spending patterns and savings rate.");
        }
        else if (endSnapshot.WealthHealthScore < 70)
        {
            actions.Add("Improve financial health by reducing discretionary spending or increasing income.");
        }
        
        // If nothing actionable, add positive reinforcement
        if (actions.Count == 0)
        {
            actions.Add("Great week! All scores improved or maintained. Keep up the momentum.");
            actions.Add("Consider setting new milestone targets to challenge yourself further.");
        }
        
        return actions.Take(3).ToList(); // Limit to 3 focus actions
    }
}
