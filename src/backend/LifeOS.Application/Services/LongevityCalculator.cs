using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

/// <summary>
/// v3.0: Longevity Calculation Engine
/// Implements multiplicative risk reduction formula per architecture.md specification.
/// Formula: combinedRiskFactor = Π(1 - r_i)
///          yearsAdded = (1 - combinedRiskFactor) × lifespanScalingFactor, capped at maxYearsCap
/// </summary>
public class LongevityCalculator : ILongevityCalculator
{
    private readonly ILifeOSDbContext _context;
    private readonly IMetricAggregationService _metricAggregationService;
    
    // Constants per design specification
    private const decimal LifespanScalingFactor = 50m;
    private const decimal MaxYearsCap = 20m;
    private const int MetricLookbackDays = 30;
    
    public LongevityCalculator(
        ILifeOSDbContext context,
        IMetricAggregationService metricAggregationService)
    {
        _context = context;
        _metricAggregationService = metricAggregationService;
    }
    
    public async Task<LongevityResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        CancellationToken cancellationToken = default)
    {
        var evaluationDate = asOfDate ?? DateTime.UtcNow;
        
        // 1. Get user baseline life expectancy
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");
        
        var baselineLifeExpectancy = user.LifeExpectancyBaseline;
        
        // 2. Get all active longevity models (system-wide + user-specific)
        var models = await _context.LongevityModels
            .Where(m => m.IsActive && (m.UserId == null || m.UserId == userId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        // 3. Get recent metric values (30-day average)
        var metricValues = await GetRecentMetricValuesAsync(userId, evaluationDate, cancellationToken);
        
        // 4. Calculate risk reduction per model
        var breakdown = new List<LongevityComponent>();
        var riskReductions = new List<decimal>();
        
        foreach (var model in models)
        {
            var riskReduction = await CalculateModelRiskReductionAsync(model, metricValues, evaluationDate, cancellationToken);
            
            if (riskReduction > 0)
            {
                riskReductions.Add(riskReduction);
                
                // Estimate individual contribution (informational only, not summed)
                var individualYears = (1.0m - (1.0m - riskReduction)) * LifespanScalingFactor;
                
                breakdown.Add(new LongevityComponent
                {
                    ModelCode = model.Code,
                    RiskReduction = riskReduction,
                    EstimatedYearsAdded = Math.Round(individualYears, 2)
                });
            }
        }
        
        // 5. Combine risk factors multiplicatively: Π(1 - r_i)
        var combinedRiskFactor = CombineRiskFactors(riskReductions);
        
        // 6. Map to years added with cap
        var (yearsAdded, adjustedLifeExpectancy) = CalculateYearsAdded(
            combinedRiskFactor, 
            baselineLifeExpectancy);
        
        // 7. Determine confidence level
        var confidence = DetermineConfidence(metricValues.Count, models.Count);
        
        return new LongevityResult
        {
            BaselineLifeExpectancyYears = baselineLifeExpectancy,
            AdjustedLifeExpectancyYears = adjustedLifeExpectancy,
            TotalYearsAdded = yearsAdded,
            RiskFactorCombined = combinedRiskFactor,
            Breakdown = breakdown,
            Confidence = confidence,
            CalculatedAt = DateTime.UtcNow
        };
    }
    
    public async Task<LongevitySnapshot> SaveSnapshotAsync(
        LongevityResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var breakdownJson = JsonSerializer.Serialize(result.Breakdown.Select(b => new
        {
            modelCode = b.ModelCode,
            riskReduction = b.RiskReduction,
            estimatedYearsAdded = b.EstimatedYearsAdded
        }));
        
        var snapshot = new LongevitySnapshot
        {
            UserId = userId,
            Timestamp = result.CalculatedAt,
            BaselineLifeExpectancyYears = result.BaselineLifeExpectancyYears,
            AdjustedLifeExpectancyYears = result.AdjustedLifeExpectancyYears,
            TotalYearsAdded = result.TotalYearsAdded,
            RiskFactorCombined = result.RiskFactorCombined,
            Breakdown = breakdownJson,
            Confidence = result.Confidence
        };
        
        _context.LongevitySnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
        
        return snapshot;
    }
    
    /// <summary>
    /// Gets recent metric values (30-day average) for longevity calculation.
    /// </summary>
    private async Task<Dictionary<string, decimal>> GetRecentMetricValuesAsync(
        Guid userId, 
        DateTime evaluationDate, 
        CancellationToken cancellationToken)
    {
        var startDate = evaluationDate.AddDays(-MetricLookbackDays);
        
        var metricCodes = await _context.MetricRecords
            .Where(r => r.UserId == userId && r.RecordedAt >= startDate && r.RecordedAt <= evaluationDate)
            .Select(r => r.MetricCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        
        var metricValues = new Dictionary<string, decimal>();
        
        foreach (var code in metricCodes)
        {
            var avgValue = await _metricAggregationService.AggregateMetricAsync(
                code,
                userId, 
                startDate, 
                evaluationDate, 
                cancellationToken);
            
            if (avgValue.HasValue)
            {
                metricValues[code] = avgValue.Value;
            }
        }
        
        return metricValues;
    }
    
    /// <summary>
    /// Calculates risk reduction for a specific model based on actual metric values.
    /// </summary>
    private async Task<decimal> CalculateModelRiskReductionAsync(
        LongevityModel model, 
        Dictionary<string, decimal> metricValues,
        DateTime evaluationDate,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse input metrics required for this model
            var inputMetrics = JsonSerializer.Deserialize<string[]>(model.InputMetrics) ?? Array.Empty<string>();
            
            // Check if we have all required metrics
            if (inputMetrics.Length == 0)
                return 0m;
            
            // For now, use the first metric (most models have single input)
            // Future: Support multi-metric models
            var primaryMetric = inputMetrics[0];
            
            if (!metricValues.TryGetValue(primaryMetric, out var actualValue))
                return 0m;
            
            // Parse model parameters
            var parameters = JsonSerializer.Deserialize<Dictionary<string, decimal>>(model.Parameters);
            if (parameters == null)
                return 0m;
            
            // Calculate risk reduction based on model type
            return model.ModelType switch
            {
                LongevityModelType.Threshold => CalculateThresholdReduction(actualValue, parameters, model.MaxRiskReduction),
                LongevityModelType.Range => CalculateRangeReduction(actualValue, parameters, model.MaxRiskReduction),
                LongevityModelType.Linear => CalculateLinearReduction(actualValue, parameters, model.MaxRiskReduction),
                LongevityModelType.Boolean => CalculateBooleanReduction(actualValue, parameters),
                _ => 0m
            };
        }
        catch
        {
            // Skip models with invalid configuration
            return 0m;
        }
    }
    
    /// <summary>
    /// Threshold model: Binary above/below threshold.
    /// Parameters: threshold, belowValue, aboveValue
    /// </summary>
    private decimal CalculateThresholdReduction(
        decimal value, 
        Dictionary<string, decimal> parameters, 
        decimal maxReduction)
    {
        if (!parameters.TryGetValue("threshold", out var threshold))
            return 0m;
        
        var belowValue = parameters.GetValueOrDefault("belowValue", 0m);
        var aboveValue = parameters.GetValueOrDefault("aboveValue", maxReduction);
        
        return value >= threshold ? aboveValue : belowValue;
    }
    
    /// <summary>
    /// Range model: Linear interpolation within optimal range, decay outside.
    /// Parameters: minOptimal, maxOptimal, minValue (optional), maxValue (optional)
    /// </summary>
    private decimal CalculateRangeReduction(
        decimal value, 
        Dictionary<string, decimal> parameters, 
        decimal maxReduction)
    {
        if (!parameters.TryGetValue("minOptimal", out var minOptimal) ||
            !parameters.TryGetValue("maxOptimal", out var maxOptimal))
            return 0m;
        
        // Within optimal range: full benefit
        if (value >= minOptimal && value <= maxOptimal)
            return maxReduction;
        
        var minValue = parameters.GetValueOrDefault("minValue", 0m);
        var maxValue = parameters.GetValueOrDefault("maxValue", 100m);
        
        // Linear decay outside optimal range
        if (value < minOptimal)
        {
            var distance = minOptimal - value;
            var range = minOptimal - minValue;
            if (range <= 0) return 0m;
            var decayFactor = Math.Max(0, 1 - distance / range);
            return maxReduction * decayFactor;
        }
        else // value > maxOptimal
        {
            var distance = value - maxOptimal;
            var range = maxValue - maxOptimal;
            if (range <= 0) return 0m;
            var decayFactor = Math.Max(0, 1 - distance / range);
            return maxReduction * decayFactor;
        }
    }
    
    /// <summary>
    /// Linear model: Proportional to value within range.
    /// Parameters: minValue, maxValue
    /// </summary>
    private decimal CalculateLinearReduction(
        decimal value, 
        Dictionary<string, decimal> parameters, 
        decimal maxReduction)
    {
        if (!parameters.TryGetValue("minValue", out var minValue) ||
            !parameters.TryGetValue("maxValue", out var maxValue))
            return 0m;
        
        if (maxValue <= minValue)
            return 0m;
        
        // Clamp value to range and normalize
        var clampedValue = Math.Clamp(value, minValue, maxValue);
        var normalized = (clampedValue - minValue) / (maxValue - minValue);
        
        return normalized * maxReduction;
    }
    
    /// <summary>
    /// Boolean model: True/false state.
    /// Parameters: trueValue, falseValue
    /// </summary>
    private decimal CalculateBooleanReduction(
        decimal value, 
        Dictionary<string, decimal> parameters)
    {
        var trueValue = parameters.GetValueOrDefault("trueValue", 0m);
        var falseValue = parameters.GetValueOrDefault("falseValue", 0m);
        
        return value > 0 ? trueValue : falseValue;
    }
    
    /// <summary>
    /// Combines risk factors multiplicatively: Π(1 - r_i)
    /// </summary>
    private decimal CombineRiskFactors(List<decimal> riskReductions)
    {
        if (riskReductions.Count == 0)
            return 1.0m; // No risk reduction = baseline risk
        
        decimal combinedRisk = 1.0m;
        
        foreach (var reduction in riskReductions)
        {
            combinedRisk *= (1.0m - reduction);
        }
        
        return combinedRisk;
    }
    
    /// <summary>
    /// Maps combined risk factor to years added with cap.
    /// Formula: yearsAdded = (1 - combinedRiskFactor) × lifespanScalingFactor, capped at maxYearsCap
    /// </summary>
    private (decimal yearsAdded, decimal adjustedLifeExpectancy) CalculateYearsAdded(
        decimal combinedRiskFactor,
        decimal baselineLifeExpectancy)
    {
        var riskImprovement = 1.0m - combinedRiskFactor;
        var yearsAddedRaw = riskImprovement * LifespanScalingFactor;
        var yearsAdded = Math.Min(yearsAddedRaw, MaxYearsCap);
        var adjustedExpectancy = baselineLifeExpectancy + yearsAdded;
        
        return (Math.Round(yearsAdded, 2), Math.Round(adjustedExpectancy, 1));
    }
    
    /// <summary>
    /// Determines confidence level based on data availability.
    /// </summary>
    private string DetermineConfidence(int metricCount, int modelCount)
    {
        if (metricCount == 0 || modelCount == 0)
            return "low";
        
        var dataRatio = (decimal)metricCount / modelCount;
        
        if (dataRatio >= 0.8m && metricCount >= 5)
            return "high";
        if (dataRatio >= 0.5m && metricCount >= 3)
            return "medium";
        
        return "low";
    }
}
