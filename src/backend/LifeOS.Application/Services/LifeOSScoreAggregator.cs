using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;

namespace LifeOS.Application.Services;

/// <summary>
/// v3.0: LifeOS Score Aggregator
/// Aggregates Health Index, Adherence Index, Wealth Health Score, and Longevity into comprehensive LifeOS Score.
/// Formula: LifeOSScore = wH × HealthIndex + wA × AdherenceIndex + wW × WealthHealthScore
/// Default weights: Health 40%, Adherence 30%, Wealth 30%
/// </summary>
public class LifeOSScoreAggregator : ILifeOSScoreAggregator
{
    private readonly IHealthIndexCalculator _healthIndexCalculator;
    private readonly IBehavioralAdherenceCalculator _adherenceCalculator;
    private readonly IWealthHealthCalculator _wealthHealthCalculator;
    private readonly ILongevityCalculator _longevityCalculator;
    private readonly IPrimaryStatsCalculator _primaryStatsCalculator;
    private readonly ILifeOSDbContext _context;
    
    // Default weights (must sum to 1.0)
    private const decimal HealthWeight = 0.4m;      // 40%
    private const decimal AdherenceWeight = 0.3m;   // 30%
    private const decimal WealthWeight = 0.3m;      // 30%
    
    public LifeOSScoreAggregator(
        IHealthIndexCalculator healthIndexCalculator,
        IBehavioralAdherenceCalculator adherenceCalculator,
        IWealthHealthCalculator wealthHealthCalculator,
        ILongevityCalculator longevityCalculator,
        IPrimaryStatsCalculator primaryStatsCalculator,
        ILifeOSDbContext context)
    {
        _healthIndexCalculator = healthIndexCalculator;
        _adherenceCalculator = adherenceCalculator;
        _wealthHealthCalculator = wealthHealthCalculator;
        _longevityCalculator = longevityCalculator;
        _primaryStatsCalculator = primaryStatsCalculator;
        _context = context;
    }
    
    public async Task<LifeOsScoreResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default)
    {
        var evaluationDate = asOfDate ?? DateTime.UtcNow;
        
        // 1. Calculate Health Index
        var healthResult = await _healthIndexCalculator.CalculateAsync(
            userId, 
            evaluationDate, 
            cancellationToken);
        
        // 2. Calculate Adherence Index (7-day lookback by default)
        var adherenceResult = await _adherenceCalculator.CalculateAsync(
            userId, 
            evaluationDate, 
            lookbackDays: 7, 
            cancellationToken: cancellationToken);
        
        // 3. Calculate Wealth Health Score
        var wealthResult = await _wealthHealthCalculator.CalculateAsync(
            userId, 
            evaluationDate, 
            cancellationToken);
        
        // 4. Calculate Longevity years added
        var longevityResult = await _longevityCalculator.CalculateAsync(
            userId, 
            evaluationDate, 
            cancellationToken);
        
        // 5. Calculate Primary Stats (includes dimension scores)
        var primaryStatsResult = await _primaryStatsCalculator.CalculateAsync(
            userId,
            evaluationDate,
            cancellationToken);
        
        // 6. Calculate weighted LifeOS Score
        // Formula: wH × HealthIndex + wA × AdherenceIndex + wW × WealthHealthScore
        var lifeScore = 
            (HealthWeight * healthResult.Score) + 
            (AdherenceWeight * adherenceResult.Score) + 
            (WealthWeight * wealthResult.Score);
        
        // 7. Map dimension scores from primary stats calculator
        var dimensionScores = primaryStatsResult.DimensionScores
            .Select(ds => new DimensionScoreEntry
            {
                DimensionCode = ds.Key,
                Score = ds.Value,
                Weight = 0m // Weight not used in primary stats context
            })
            .ToList();
        
        return new LifeOsScoreResult
        {
            LifeScore = Math.Round(lifeScore, 2),
            HealthIndex = healthResult.Score,
            AdherenceIndex = adherenceResult.Score,
            WealthHealthScore = wealthResult.Score,
            LongevityYearsAdded = longevityResult.TotalYearsAdded,
            DimensionScores = dimensionScores,
            CalculatedAt = DateTime.UtcNow
        };
    }
    
    public async Task<LifeOsScoreSnapshot> SaveSnapshotAsync(
        LifeOsScoreResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        // Serialize dimension scores to JSON
        var dimensionScoresJson = System.Text.Json.JsonSerializer.Serialize(
            result.DimensionScores.Select(d => new 
            {
                dimensionCode = d.DimensionCode,
                score = d.Score,
                weight = d.Weight
            }));
        
        var snapshot = new LifeOsScoreSnapshot
        {
            UserId = userId,
            Timestamp = result.CalculatedAt,
            LifeScore = result.LifeScore,
            HealthIndex = result.HealthIndex,
            AdherenceIndex = result.AdherenceIndex,
            WealthHealthScore = result.WealthHealthScore,
            LongevityYearsAdded = result.LongevityYearsAdded,
            DimensionScores = dimensionScoresJson
        };
        
        _context.LifeOsScoreSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
        
        return snapshot;
    }
}
