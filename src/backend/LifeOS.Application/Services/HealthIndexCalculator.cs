using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Services;

/// <summary>
/// v3.0: Health Index Calculator with per-metric normalization
/// Implements precise scoring formulas based on target directions
/// </summary>
public class HealthIndexCalculator : IHealthIndexCalculator
{
    private readonly ILifeOSDbContext _context;
    private readonly IMetricAggregationService _metricAggregationService;
    private readonly ILogger<HealthIndexCalculator> _logger;

    public HealthIndexCalculator(
        ILifeOSDbContext context,
        IMetricAggregationService metricAggregationService,
        ILogger<HealthIndexCalculator> logger)
    {
        _context = context;
        _metricAggregationService = metricAggregationService;
        _logger = logger;
    }

    public async Task<HealthIndexResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default)
    {
        var evaluationDate = asOfDate ?? DateTime.UtcNow;
        var lookbackDays = 7; // configurable in future
        var startDate = evaluationDate.AddDays(-lookbackDays);
        
        _logger.LogInformation("Calculating Health Index for user {UserId} as of {Date} (lookback: {Days} days)", 
            userId, evaluationDate, lookbackDays);
        
        // 1. Load health metric definitions
        var healthMetrics = await LoadHealthMetricsAsync(userId, cancellationToken);
        
        if (healthMetrics.Count == 0)
        {
            _logger.LogWarning("No health metrics configured for user {UserId}", userId);
            return new HealthIndexResult
            {
                Score = 0m,
                Components = new List<HealthMetricScore>(),
                CalculatedAt = DateTime.UtcNow
            };
        }
        
        var components = new List<HealthMetricScore>();
        
        foreach (var metric in healthMetrics)
        {
            // 2. Aggregate metric value
            var aggregatedValue = await _metricAggregationService.AggregateMetricAsync(
                metric.Code,
                userId,
                startDate,
                evaluationDate,
                cancellationToken);
            
            if (!aggregatedValue.HasValue)
            {
                _logger.LogDebug("No data for metric {MetricCode}, skipping", metric.Code);
                continue; // Skip metrics with no data
            }
            
            // 3. Score the value
            var score = ScoreMetric(metric, aggregatedValue.Value);
            
            _logger.LogDebug("Metric {MetricCode}: actual={ActualValue}, target={TargetValue}, score={Score}, weight={Weight}",
                metric.Code, aggregatedValue.Value, metric.TargetValue, score, metric.Weight);
            
            // 4. Add component
            components.Add(new HealthMetricScore
            {
                MetricCode = metric.Code,
                Score = score,
                Weight = metric.Weight,
                ActualValue = aggregatedValue.Value,
                TargetValue = metric.TargetValue
            });
        }
        
        // 5. Calculate weighted average
        var totalWeight = components.Sum(c => c.Weight);
        var weightedScore = totalWeight > 0 
            ? components.Sum(c => c.Score * c.Weight) / totalWeight
            : 0m;
        
        _logger.LogInformation("Health Index calculated: {Score}/100 from {ComponentCount} metrics (total weight: {TotalWeight})",
            Math.Round(weightedScore, 2), components.Count, totalWeight);
        
        return new HealthIndexResult
        {
            Score = Math.Round(weightedScore, 2),
            Components = components,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private async Task<List<MetricDefinitionDto>> LoadHealthMetricsAsync(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // Load metric definitions tagged with "health" or in "health_recovery" dimension
        var metrics = await _context.MetricDefinitions
            .AsNoTracking()
            .Where(m => m.IsActive 
                && (m.Tags != null && m.Tags.Contains("health") 
                    || m.Dimension != null && m.Dimension.Code == "health_recovery"))
            .Select(m => new MetricDefinitionDto
            {
                Code = m.Code,
                TargetDirection = m.TargetDirection,
                TargetValue = m.TargetValue,
                MinValue = m.MinValue,
                MaxValue = m.MaxValue,
                Weight = m.Weight
            })
            .ToListAsync(cancellationToken);
        
        return metrics;
    }

    private decimal ScoreMetric(MetricDefinitionDto metric, decimal actualValue)
    {
        return metric.TargetDirection switch
        {
            TargetDirection.AtOrBelow => ScoreAtOrBelow(actualValue, metric.TargetValue!.Value, metric.MaxValue!.Value),
            TargetDirection.AtOrAbove => ScoreAtOrAbove(actualValue, metric.TargetValue!.Value, metric.MinValue!.Value),
            TargetDirection.Range => ScoreRange(actualValue, metric.MinValue!.Value, metric.MaxValue!.Value),
            _ => 0m
        };
    }
    
    private class MetricDefinitionDto
    {
        public string Code { get; set; } = string.Empty;
        public TargetDirection TargetDirection { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal Weight { get; set; }
    }

    /// <summary>
    /// Score metric where lower is better (AtOrBelow).
    /// Examples: resting heart rate, body weight, blood pressure
    /// </summary>
    /// <param name="actualValue">Current measured value</param>
    /// <param name="targetValue">Optimal target value (ideal threshold)</param>
    /// <param name="maxValue">Maximum acceptable value (worst case)</param>
    /// <returns>Score from 0-100</returns>
    protected virtual decimal ScoreAtOrBelow(decimal actualValue, decimal targetValue, decimal maxValue)
    {
        // If at or below target: perfect score
        if (actualValue <= targetValue) 
            return 100m;
        
        // If at or above max: worst score
        if (actualValue >= maxValue) 
            return 0m;
        
        // Linear interpolation between target and max
        return (maxValue - actualValue) / (maxValue - targetValue) * 100m;
    }

    /// <summary>
    /// Score metric where higher is better (AtOrAbove).
    /// Examples: HRV, VO2 max, steps, sleep hours
    /// </summary>
    /// <param name="actualValue">Current measured value</param>
    /// <param name="targetValue">Optimal target value (ideal threshold)</param>
    /// <param name="minValue">Minimum acceptable value (worst case)</param>
    /// <returns>Score from 0-100</returns>
    protected virtual decimal ScoreAtOrAbove(decimal actualValue, decimal targetValue, decimal minValue)
    {
        // If at or above target: perfect score
        if (actualValue >= targetValue) 
            return 100m;
        
        // If at or below min: worst score
        if (actualValue <= minValue) 
            return 0m;
        
        // Linear interpolation between min and target
        return (actualValue - minValue) / (targetValue - minValue) * 100m;
    }

    /// <summary>
    /// Score metric where an optimal range is desired (Range).
    /// Examples: body fat % (13-15%), blood glucose (70-100 mg/dL)
    /// </summary>
    /// <param name="actualValue">Current measured value</param>
    /// <param name="minValue">Minimum optimal value (lower bound of ideal range)</param>
    /// <param name="maxValue">Maximum optimal value (upper bound of ideal range)</param>
    /// <param name="toleranceFactor">How far outside range before score reaches 0 (default 0.2 = 20% of range size)</param>
    /// <returns>Score from 0-100</returns>
    protected virtual decimal ScoreRange(
        decimal actualValue, 
        decimal minValue, 
        decimal maxValue, 
        decimal toleranceFactor = 0.2m)
    {
        // If within optimal range: perfect score
        if (actualValue >= minValue && actualValue <= maxValue) 
            return 100m;
        
        // Calculate distance from range
        decimal distance = actualValue < minValue 
            ? minValue - actualValue 
            : actualValue - maxValue;
        
        // Calculate tolerance threshold
        decimal rangeSize = maxValue - minValue;
        decimal tolerance = rangeSize * toleranceFactor;
        
        // If beyond tolerance: worst score
        if (distance >= tolerance) 
            return 0m;
        
        // Linear decay within tolerance zone
        return (tolerance - distance) / tolerance * 100m;
    }
    
    public async Task<HealthIndexSnapshot> SaveSnapshotAsync(
        HealthIndexResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var snapshot = new HealthIndexSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Timestamp = result.CalculatedAt,
            Score = result.Score,
            Components = JsonSerializer.Serialize(result.Components),
            CreatedAt = DateTime.UtcNow
        };
        
        _context.HealthIndexSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Saved Health Index snapshot {SnapshotId} for user {UserId} with score {Score}",
            snapshot.Id, userId, snapshot.Score);
        
        return snapshot;
    }
}
