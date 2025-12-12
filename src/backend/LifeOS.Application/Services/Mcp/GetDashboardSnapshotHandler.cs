using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Interfaces.Mcp;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.getDashboardSnapshot
/// Retrieves complete dashboard state (scores, stats, tasks, events) in a single call.
/// </summary>
public class GetDashboardSnapshotHandler : IMcpToolHandler
{
    private readonly ILifeOSScoreAggregator _scoreAggregator;
    private readonly IPrimaryStatsCalculator _statsCalculator;
    private readonly ILifeOSDbContext _context;
    
    public string ToolName => "lifeos.getDashboardSnapshot";
    public string Description => "Get comprehensive dashboard snapshot with scores, stats, tasks, and events";
    
    public GetDashboardSnapshotHandler(
        ILifeOSScoreAggregator scoreAggregator,
        IPrimaryStatsCalculator statsCalculator,
        ILifeOSDbContext context)
    {
        _scoreAggregator = scoreAggregator;
        _statsCalculator = statsCalculator;
        _context = context;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get latest LifeOS Score (or calculate if not exists)
            var scoreResult = await GetOrCalculateScoreAsync(userId, cancellationToken);
            
            // 2. Get primary stats
            var statsResult = await _statsCalculator.CalculateAsync(userId, DateTime.UtcNow, cancellationToken);
            
            // 3. Get today's tasks
            var todayTasks = await GetTodayTasksAsync(userId, cancellationToken);
            
            // 4. Get net worth from latest metric
            var netWorth = await GetNetWorthAsync(userId, cancellationToken);
            
            // 5. Check for upcoming key events
            var nextKeyEvents = await GetKeyEventsAsync(userId, cancellationToken);
            
            // 6. Build snapshot
            var snapshot = new DashboardSnapshotDto
            {
                LifeScore = scoreResult.LifeScore,
                HealthIndex = scoreResult.HealthIndex,
                AdherenceIndex = scoreResult.AdherenceIndex,
                WealthHealthScore = scoreResult.WealthHealthScore,
                LongevityYearsAdded = scoreResult.LongevityYearsAdded,
                PrimaryStats = statsResult.Values.ToDictionary(
                    kv => kv.Key, 
                    kv => (int)Math.Round(kv.Value)),
                Dimensions = scoreResult.DimensionScores
                    .Select(d => new DimensionScoreDto 
                    { 
                        Code = d.DimensionCode, 
                        Score = d.Score 
                    })
                    .ToList(),
                TodayTasks = todayTasks,
                NetWorthHomeCcy = netWorth,
                NextKeyEvents = nextKeyEvents
            };
            
            return McpToolResponse<object>.Ok(snapshot);
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to get dashboard snapshot: {ex.Message}");
        }
    }
    
    private async Task<LifeOsScoreResult> GetOrCalculateScoreAsync(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // Try to get latest snapshot first
        var latestSnapshot = await _context.LifeOsScoreSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
        
        // If snapshot exists and is recent (within 1 hour), use it
        if (latestSnapshot != null && 
            (DateTime.UtcNow - latestSnapshot.Timestamp).TotalHours < 1)
        {
            return new LifeOsScoreResult
            {
                LifeScore = latestSnapshot.LifeScore,
                HealthIndex = latestSnapshot.HealthIndex,
                AdherenceIndex = latestSnapshot.AdherenceIndex,
                WealthHealthScore = latestSnapshot.WealthHealthScore,
                LongevityYearsAdded = latestSnapshot.LongevityYearsAdded,
                DimensionScores = new List<DimensionScoreEntry>(), // Will be populated by primary stats
                CalculatedAt = latestSnapshot.Timestamp
            };
        }
        
        // Otherwise, calculate fresh
        return await _scoreAggregator.CalculateAsync(userId, DateTime.UtcNow, cancellationToken);
    }
    
    private async Task<List<TodayTaskDto>> GetTodayTasksAsync(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Get daily tasks and tasks scheduled for today
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.IsActive)
            .Where(t => t.Frequency == Frequency.Daily || t.ScheduledDate == today)
            .Include(t => t.Dimension)
            .OrderBy(t => t.Frequency)
            .ThenBy(t => t.ScheduledTime)
            .Take(20)
            .ToListAsync(cancellationToken);
        
        // Get task IDs for completion check
        var taskIds = tasks.Select(t => t.Id).ToList();
        
        // Get completions for today
        var completedTaskIds = await _context.TaskCompletions
            .Where(c => taskIds.Contains(c.TaskId))
            .Where(c => DateOnly.FromDateTime(c.CompletedAt) == today)
            .Select(c => c.TaskId)
            .Distinct()
            .ToListAsync(cancellationToken);
        
        // Build DTOs
        return tasks.Select(t => new TodayTaskDto
        {
            TaskId = t.Id,
            DimensionCode = t.Dimension?.Code ?? "uncategorized",
            Title = t.Title,
            IsCompleted = completedTaskIds.Contains(t.Id),
            Frequency = t.Frequency.ToString().ToLower()
        }).ToList();
    }
    
    private async Task<decimal?> GetNetWorthAsync(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // Get latest net worth metric
        var netWorthMetric = await _context.MetricRecords
            .Where(m => m.UserId == userId && m.MetricCode == "net_worth_homeccy")
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
        
        return netWorthMetric?.ValueNumber;
    }
    
    private async Task<List<KeyEventDto>> GetKeyEventsAsync(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        var events = new List<KeyEventDto>();
        
        // Check for weekly review due
        var lastReview = await _context.MetricRecords
            .Where(m => m.UserId == userId && m.MetricCode == "system.weekly_review_done")
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
        
        var daysSinceReview = lastReview == null 
            ? 999 
            : (DateTime.UtcNow - lastReview.RecordedAt).Days;
        
        if (daysSinceReview >= 7)
        {
            // Calculate next Sunday
            var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)DateTime.UtcNow.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0) daysUntilSunday = 7;
            
            var nextSunday = DateTime.UtcNow.AddDays(daysUntilSunday);
            
            events.Add(new KeyEventDto
            {
                Type = "weekly_review_due",
                Date = nextSunday.ToString("yyyy-MM-dd")
            });
        }
        
        return events;
    }
}
