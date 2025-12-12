using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Services;

/// <summary>
/// v3.0: Primary Stats Calculator
/// Calculates primary stats (strength, wisdom, charisma, composure, energy, influence, vitality)
/// from dimension scores using weighted formulas.
/// </summary>
public class PrimaryStatsCalculator : IPrimaryStatsCalculator
{
    private readonly ILifeOSDbContext _context;
    private readonly IMetricAggregationService _metricAggregationService;
    private readonly ILogger<PrimaryStatsCalculator> _logger;
    
    // Weights for dimension score calculation
    private const decimal MetricsWeight = 0.6m;     // 60% from metrics
    private const decimal AdherenceWeight = 0.4m;   // 40% from task adherence
    
    public PrimaryStatsCalculator(
        ILifeOSDbContext context,
        IMetricAggregationService metricAggregationService,
        ILogger<PrimaryStatsCalculator> logger)
    {
        _context = context;
        _metricAggregationService = metricAggregationService;
        _logger = logger;
    }
    
    public async Task<PrimaryStatsResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default)
    {
        var evaluationDate = asOfDate ?? DateTime.UtcNow;
        
        _logger.LogInformation("Calculating Primary Stats for user {UserId} as of {Date}", 
            userId, evaluationDate);
        
        // 1. Calculate dimension scores
        var dimensionScores = await CalculateDimensionScoresAsync(userId, evaluationDate, cancellationToken);
        
        // 2. Calculate primary stats from dimension scores
        var primaryStatValues = await CalculatePrimaryStatsAsync(dimensionScores, cancellationToken);
        
        return new PrimaryStatsResult
        {
            Values = primaryStatValues,
            DimensionScores = dimensionScores,
            CalculatedAt = evaluationDate,
            CalculationDetails = new Dictionary<string, object>
            {
                { "metrics_weight", MetricsWeight },
                { "adherence_weight", AdherenceWeight },
                { "dimension_count", dimensionScores.Count },
                { "stat_count", primaryStatValues.Count }
            }
        };
    }
    
    public async Task<PrimaryStatRecord> SaveSnapshotAsync(
        PrimaryStatsResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var snapshot = new PrimaryStatRecord
        {
            UserId = userId,
            RecordedAt = result.CalculatedAt,
            Strength = (int)Math.Round(result.Values.GetValueOrDefault("strength", 0m)),
            Wisdom = (int)Math.Round(result.Values.GetValueOrDefault("wisdom", 0m)),
            Charisma = (int)Math.Round(result.Values.GetValueOrDefault("charisma", 0m)),
            Composure = (int)Math.Round(result.Values.GetValueOrDefault("composure", 0m)),
            Energy = (int)Math.Round(result.Values.GetValueOrDefault("energy", 0m)),
            Influence = (int)Math.Round(result.Values.GetValueOrDefault("influence", 0m)),
            Vitality = (int)Math.Round(result.Values.GetValueOrDefault("vitality", 0m)),
            CalculationDetails = JsonSerializer.Serialize(new
            {
                dimension_scores = result.DimensionScores,
                calculation_details = result.CalculationDetails
            })
        };
        
        _context.PrimaryStatRecords.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Saved Primary Stats snapshot for user {UserId} at {Date}", 
            userId, result.CalculatedAt);
        
        return snapshot;
    }
    
    /// <summary>
    /// Calculates dimension scores from metrics and task adherence.
    /// Formula: DimensionScore = (w_metrics × avg(metric_scores) + w_adherence × adherence) / (w_metrics + w_adherence)
    /// </summary>
    private async Task<Dictionary<string, decimal>> CalculateDimensionScoresAsync(
        Guid userId, 
        DateTime asOfDate,
        CancellationToken cancellationToken)
    {
        var dimensions = await _context.Dimensions
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);
        
        var dimensionScores = new Dictionary<string, decimal>();
        var lookbackDays = 7; // Last 7 days for metrics
        var startDate = asOfDate.AddDays(-lookbackDays);
        
        foreach (var dimension in dimensions)
        {
            // 1. Calculate metric scores for this dimension
            var metricScores = await CalculateMetricScoresForDimensionAsync(
                userId, 
                dimension.Id, 
                startDate, 
                asOfDate,
                cancellationToken);
            
            // 2. Calculate task adherence for this dimension
            var adherence = await CalculateDimensionAdherenceAsync(
                userId, 
                dimension.Id, 
                startDate, 
                asOfDate,
                cancellationToken);
            
            // 3. Weighted blend
            var avgMetricScore = metricScores.Any() ? metricScores.Average() : 50m; // Default to 50 if no metrics
            var dimensionScore = (MetricsWeight * avgMetricScore + AdherenceWeight * adherence) / 
                               (MetricsWeight + AdherenceWeight);
            
            dimensionScores[dimension.Code] = Math.Round(dimensionScore, 2);
            
            _logger.LogDebug("Dimension {Code}: AvgMetric={Metric}, Adherence={Adherence}, Score={Score}",
                dimension.Code, avgMetricScore, adherence, dimensionScore);
        }
        
        return dimensionScores;
    }
    
    /// <summary>
    /// Calculates metric scores for a specific dimension.
    /// </summary>
    private async Task<List<decimal>> CalculateMetricScoresForDimensionAsync(
        Guid userId,
        Guid dimensionId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var metrics = await _context.MetricDefinitions
            .Where(m => m.DimensionId == dimensionId && m.IsActive)
            .ToListAsync(cancellationToken);
        
        var metricScores = new List<decimal>();
        
        foreach (var metric in metrics)
        {
            // Get recent metric value using aggregation service
            var recentValue = await _metricAggregationService.AggregateMetricAsync(
                metric.Code,
                userId,
                startDate,
                endDate,
                cancellationToken);
            
            if (recentValue.HasValue)
            {
                // Normalize metric value to 0-100 score
                var score = NormalizeMetricValue(metric, recentValue.Value);
                metricScores.Add(score);
            }
        }
        
        return metricScores;
    }
    
    /// <summary>
    /// Normalizes a metric value to a 0-100 score based on metric configuration.
    /// Uses min/max values from MetricDefinition.
    /// </summary>
    private decimal NormalizeMetricValue(MetricDefinition metric, decimal value)
    {
        // Use MinValue and MaxValue from MetricDefinition
        decimal minValue = metric.MinValue ?? 0m;
        decimal maxValue = metric.MaxValue ?? 100m;
        
        // Normalize to 0-100
        if (maxValue <= minValue)
            return 50m; // Invalid range, default to 50
        
        var normalizedScore = ((value - minValue) / (maxValue - minValue)) * 100m;
        
        // Clamp to 0-100
        return Math.Max(0m, Math.Min(100m, normalizedScore));
    }
    
    /// <summary>
    /// Calculates task adherence percentage for a dimension.
    /// Formula: (completed_tasks / total_tasks) × 100
    /// </summary>
    private async Task<decimal> CalculateDimensionAdherenceAsync(
        Guid userId,
        Guid dimensionId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);
        
        // Get all tasks in this dimension within the date range
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && 
                       t.DimensionId == dimensionId &&
                       t.ScheduledDate.HasValue &&
                       t.ScheduledDate >= startDateOnly &&
                       t.ScheduledDate <= endDateOnly)
            .ToListAsync(cancellationToken);
        
        if (!tasks.Any())
            return 50m; // No tasks = neutral score
        
        // Get task completions
        var taskIds = tasks.Select(t => t.Id).ToList();
        var completions = await _context.TaskCompletions
            .Where(tc => taskIds.Contains(tc.TaskId) &&
                        tc.CompletedAt >= startDate &&
                        tc.CompletedAt <= endDate)
            .Select(tc => tc.TaskId)
            .Distinct()
            .ToListAsync(cancellationToken);
        
        var completedCount = completions.Count;
        var totalCount = tasks.Count;
        
        var adherencePercentage = (decimal)completedCount / totalCount * 100m;
        
        return Math.Round(adherencePercentage, 2);
    }
    
    /// <summary>
    /// Calculates primary stats from dimension scores using weighted formulas.
    /// Formula: PrimaryStatValue(stat) = Σ(DimensionScore(dim) × Weight(dim, stat)) / Σ(Weight(dim, stat))
    /// </summary>
    private async Task<Dictionary<string, decimal>> CalculatePrimaryStatsAsync(
        Dictionary<string, decimal> dimensionScores,
        CancellationToken cancellationToken)
    {
        var primaryStats = await _context.PrimaryStats
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);
        
        var weights = await _context.DimensionPrimaryStatWeights
            .Include(w => w.Dimension)
            .ToListAsync(cancellationToken);
        
        var statValues = new Dictionary<string, decimal>();
        
        foreach (var stat in primaryStats)
        {
            // Get all weights for this stat
            var statWeights = weights.Where(w => w.PrimaryStatCode == stat.Code).ToList();
            
            if (!statWeights.Any())
            {
                _logger.LogWarning("No weights found for stat {Code}, defaulting to 0", stat.Code);
                statValues[stat.Code] = 0m;
                continue;
            }
            
            decimal weightedSum = 0m;
            decimal totalWeight = 0m;
            
            foreach (var weight in statWeights)
            {
                if (dimensionScores.TryGetValue(weight.Dimension.Code, out var dimScore))
                {
                    weightedSum += dimScore * weight.Weight;
                    totalWeight += weight.Weight;
                }
            }
            
            // Calculate weighted average
            var statValue = totalWeight > 0 ? weightedSum / totalWeight : 0m;
            statValues[stat.Code] = Math.Round(statValue, 2);
            
            _logger.LogDebug("Stat {Code}: WeightedSum={Sum}, TotalWeight={Weight}, Value={Value}",
                stat.Code, weightedSum, totalWeight, statValue);
        }
        
        return statValues;
    }
}
