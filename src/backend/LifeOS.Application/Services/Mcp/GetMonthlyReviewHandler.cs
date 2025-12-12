using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Interfaces.Mcp;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.getMonthlyReview
/// Retrieves monthly performance review with LifeOS score trend, net worth change, 
/// identity radar comparison, and milestone progress.
/// </summary>
public class GetMonthlyReviewHandler : IMcpToolHandler
{
    private readonly ILifeOSDbContext _context;
    private readonly IPrimaryStatsCalculator _statsCalculator;
    
    public string ToolName => "lifeos.getMonthlyReview";
    public string Description => "Get monthly performance review with score trends, net worth changes, identity radar, and milestone progress";
    
    public GetMonthlyReviewHandler(
        ILifeOSDbContext context,
        IPrimaryStatsCalculator statsCalculator)
    {
        _context = context;
        _statsCalculator = statsCalculator;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse input
            MonthlyReviewRequestDto? request = null;
            if (!string.IsNullOrEmpty(jsonInput))
            {
                request = JsonSerializer.Deserialize<MonthlyReviewRequestDto>(
                    jsonInput, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            // Determine month period
            var (monthStart, monthEnd) = GetMonthPeriod(request);
            
            // Get score snapshots for month start and end
            var monthStartSnapshot = await GetSnapshotNearDate(userId, monthStart, cancellationToken);
            var monthEndSnapshot = await GetSnapshotNearDate(userId, monthEnd, cancellationToken);
            
            // Get net worth change
            var netWorthChange = await GetNetWorthChange(userId, monthStart, monthEnd, cancellationToken);
            
            // Get identity radar comparison
            var identityRadarComparison = await GetIdentityRadarComparison(userId, cancellationToken);
            
            // Get milestone progress
            var milestoneProgress = await GetMilestoneProgress(userId, cancellationToken);
            
            // Generate top wins
            var topWins = GenerateTopWins(monthStartSnapshot, monthEndSnapshot, netWorthChange);
            
            // Build response
            var review = new MonthlyReviewDto
            {
                Period = new PeriodDto
                {
                    Start = monthStart.ToString("yyyy-MM-dd"),
                    End = monthEnd.ToString("yyyy-MM-dd")
                },
                LifeScoreChange = monthStartSnapshot != null && monthEndSnapshot != null
                    ? new ScoreChangeDto 
                    { 
                        From = Math.Round(monthStartSnapshot.LifeScore, 0), 
                        To = Math.Round(monthEndSnapshot.LifeScore, 0) 
                    }
                    : null,
                NetWorthChange = netWorthChange,
                LongevityChange = monthStartSnapshot != null && monthEndSnapshot != null
                    ? new LongevityChangeDto 
                    { 
                        From = Math.Round(monthStartSnapshot.LongevityYearsAdded, 1), 
                        To = Math.Round(monthEndSnapshot.LongevityYearsAdded, 1) 
                    }
                    : null,
                IdentityRadarComparison = identityRadarComparison,
                MilestoneProgress = milestoneProgress,
                TopWins = topWins
            };
            
            return McpToolResponse<object>.Ok(review);
        }
        catch (JsonException ex)
        {
            return McpToolResponse<object>.Fail($"Invalid JSON input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to get monthly review: {ex.Message}");
        }
    }
    
    private (DateTime monthStart, DateTime monthEnd) GetMonthPeriod(MonthlyReviewRequestDto? request)
    {
        var year = request?.Year ?? DateTime.UtcNow.Year;
        var month = 1;
        
        if (!string.IsNullOrEmpty(request?.Month))
        {
            // Parse month from "YYYY-MM" format or just "MM"
            var parts = request.Month.Split('-');
            if (parts.Length == 2)
            {
                year = int.Parse(parts[0]);
                month = int.Parse(parts[1]);
            }
            else
            {
                month = int.Parse(parts[0]);
            }
        }
        else
        {
            month = DateTime.UtcNow.Month;
        }
        
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        
        return (monthStart, monthEnd);
    }
    
    private async Task<Domain.Entities.LifeOsScoreSnapshot?> GetSnapshotNearDate(
        Guid userId, 
        DateTime targetDate, 
        CancellationToken cancellationToken)
    {
        // Find snapshot within 3 days of target date
        var snapshot = await _context.LifeOsScoreSnapshots
            .Where(s => s.UserId == userId)
            .Where(s => s.Timestamp >= targetDate.AddDays(-3) && s.Timestamp <= targetDate.AddDays(3))
            .OrderBy(s => Math.Abs((s.Timestamp - targetDate).Ticks))
            .FirstOrDefaultAsync(cancellationToken);
        
        return snapshot;
    }
    
    private async Task<NetWorthChangeDto?> GetNetWorthChange(
        Guid userId, 
        DateTime monthStart, 
        DateTime monthEnd, 
        CancellationToken cancellationToken)
    {
        // Get net worth at start and end of month
        var startNetWorth = await _context.MetricRecords
            .Where(m => m.UserId == userId && m.MetricCode == "net_worth_homeccy")
            .Where(m => m.RecordedAt <= monthStart.AddDays(3))
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
        
        var endNetWorth = await _context.MetricRecords
            .Where(m => m.UserId == userId && m.MetricCode == "net_worth_homeccy")
            .Where(m => m.RecordedAt <= monthEnd.AddDays(1))
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (startNetWorth == null || endNetWorth == null)
            return null;
        
        return new NetWorthChangeDto
        {
            From = startNetWorth.ValueNumber ?? 0,
            To = endNetWorth.ValueNumber ?? 0
        };
    }
    
    private async Task<IdentityRadarComparisonDto?> GetIdentityRadarComparison(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // Get current primary stats
        var currentStats = await _statsCalculator.CalculateAsync(userId, DateTime.UtcNow, cancellationToken);
        
        // Get identity profile targets
        var identityProfile = await _context.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        
        if (identityProfile == null)
            return null;
        
        var targets = JsonSerializer.Deserialize<Dictionary<string, int>>(identityProfile.PrimaryStatTargets) 
            ?? new Dictionary<string, int>();
        
        return new IdentityRadarComparisonDto
        {
            Current = new PrimaryStatsDto
            {
                Strength = (int)Math.Round(currentStats.Values.GetValueOrDefault("strength", 0)),
                Wisdom = (int)Math.Round(currentStats.Values.GetValueOrDefault("wisdom", 0)),
                Charisma = (int)Math.Round(currentStats.Values.GetValueOrDefault("charisma", 0)),
                Composure = (int)Math.Round(currentStats.Values.GetValueOrDefault("composure", 0)),
                Energy = (int)Math.Round(currentStats.Values.GetValueOrDefault("energy", 0)),
                Influence = (int)Math.Round(currentStats.Values.GetValueOrDefault("influence", 0)),
                Vitality = (int)Math.Round(currentStats.Values.GetValueOrDefault("vitality", 0))
            },
            Targets = new PrimaryStatsDto
            {
                Strength = targets.GetValueOrDefault("strength", 0),
                Wisdom = targets.GetValueOrDefault("wisdom", 0),
                Charisma = targets.GetValueOrDefault("charisma", 0),
                Composure = targets.GetValueOrDefault("composure", 0),
                Energy = targets.GetValueOrDefault("energy", 0),
                Influence = targets.GetValueOrDefault("influence", 0),
                Vitality = targets.GetValueOrDefault("vitality", 0)
            }
        };
    }
    
    private async Task<List<MilestoneProgressDto>> GetMilestoneProgress(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // Get active milestones with progress calculation
        var milestones = await _context.Milestones
            .Where(m => m.UserId == userId && m.Status == Domain.Enums.MilestoneStatus.Active)
            .OrderBy(m => m.TargetDate)
            .Take(5)
            .ToListAsync(cancellationToken);
        
        return milestones.Select(m =>
        {
            // Simple progress calculation: percentage based on current vs target metric
            var percentComplete = CalculateMilestoneProgress(m);
            
            return new MilestoneProgressDto
            {
                MilestoneTitle = m.Title,
                PercentComplete = percentComplete,
                TargetDate = m.TargetDate?.ToString("yyyy-MM-dd")
            };
        }).ToList();
    }
    
    private int CalculateMilestoneProgress(Domain.Entities.Milestone milestone)
    {
        // Simplified progress calculation
        // In a real implementation, this would check linked metrics
        // For now, return a reasonable estimate based on completion status
        if (milestone.Status == Domain.Enums.MilestoneStatus.Completed) return 100;
        
        // If target date is set, estimate based on time elapsed
        if (milestone.TargetDate.HasValue)
        {
            var targetDate = milestone.TargetDate.Value.ToDateTime(TimeOnly.MinValue);
            var createdDate = milestone.CreatedAt;
            var totalDays = (targetDate - createdDate).Days;
            var elapsedDays = (DateTime.UtcNow - createdDate).Days;
            
            if (totalDays > 0)
            {
                var timeProgress = Math.Min(100, (int)((double)elapsedDays / totalDays * 100));
                // Assume 50% of time-based progress (optimistic estimate)
                return Math.Min(95, timeProgress / 2);
            }
        }
        
        // Default estimate for milestones without target date
        return 30;
    }
    
    private List<string> GenerateTopWins(
        Domain.Entities.LifeOsScoreSnapshot? startSnapshot,
        Domain.Entities.LifeOsScoreSnapshot? endSnapshot,
        NetWorthChangeDto? netWorthChange)
    {
        var wins = new List<string>();
        
        if (startSnapshot == null || endSnapshot == null)
        {
            wins.Add("Complete monthly review to establish progress tracking.");
            return wins;
        }
        
        // LifeOS Score improvements
        var lifeScoreImprovement = endSnapshot.LifeScore - startSnapshot.LifeScore;
        if (lifeScoreImprovement >= 5)
        {
            wins.Add($"LifeOS Score increased by {Math.Round(lifeScoreImprovement, 1)} points");
        }
        
        // Net worth improvements
        if (netWorthChange != null)
        {
            var netWorthIncrease = netWorthChange.To - netWorthChange.From;
            if (netWorthIncrease > 0)
            {
                wins.Add($"Net worth increased by {FormatCurrency(netWorthIncrease)}");
            }
        }
        
        // Health improvements
        var healthImprovement = endSnapshot.HealthIndex - startSnapshot.HealthIndex;
        if (healthImprovement >= 5)
        {
            wins.Add($"Health Index improved by {Math.Round(healthImprovement, 1)} points");
        }
        
        // Adherence improvements
        var adherenceImprovement = endSnapshot.AdherenceIndex - startSnapshot.AdherenceIndex;
        if (adherenceImprovement >= 5)
        {
            wins.Add($"Task adherence improved by {Math.Round(adherenceImprovement, 1)} points");
        }
        
        // Longevity improvements
        var longevityImprovement = endSnapshot.LongevityYearsAdded - startSnapshot.LongevityYearsAdded;
        if (longevityImprovement >= 0.5M)
        {
            wins.Add($"Longevity increased by {Math.Round(longevityImprovement, 1)} years");
        }
        
        // If no wins, provide encouragement
        if (wins.Count == 0)
        {
            wins.Add("Focus on consistency this month to see measurable improvements");
            wins.Add("Small daily improvements compound into significant progress");
        }
        
        return wins.Take(5).ToList(); // Top 5 wins
    }
    
    private string FormatCurrency(decimal amount)
    {
        if (amount >= 1000000)
            return $"${Math.Round(amount / 1000000, 1)}M";
        if (amount >= 1000)
            return $"${Math.Round(amount / 1000, 1)}k";
        return $"${Math.Round(amount, 0)}";
    }
}
