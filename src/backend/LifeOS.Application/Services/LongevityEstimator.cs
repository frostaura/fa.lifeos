using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

public interface ILongevityEstimator
{
    Task<LongevityEstimate> CalculateEstimateAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record LongevityEstimate(
    decimal BaselineLifeExpectancy,
    decimal EstimatedYearsAdded,
    decimal AdjustedLifeExpectancy,
    DateTime? EstimatedDeathDate,
    string ConfidenceLevel,
    DateTime CalculatedAt,
    List<LongevityBreakdownItem> Breakdown,
    List<LongevityRecommendation> Recommendations
);

public record LongevityBreakdownItem(
    string ModelCode,
    string ModelName,
    decimal YearsAdded,
    Dictionary<string, object?> InputValues,
    string? Notes
);

public record LongevityRecommendation(
    string Area,
    string Suggestion,
    decimal PotentialGain
);

public class LongevityEstimator : ILongevityEstimator
{
    private readonly ILifeOSDbContext _context;

    public LongevityEstimator(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<LongevityEstimate> CalculateEstimateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        // Get user's baseline life expectancy (default 80 years)
        var baseline = user.LifeExpectancyBaseline;

        // Get all active longevity models
        var models = await _context.LongevityModels
            .Where(m => m.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get the most recent metric values (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentMetrics = await _context.MetricRecords
            .Where(r => r.UserId == userId && r.RecordedAt >= thirtyDaysAgo)
            .GroupBy(r => r.MetricCode)
            .Select(g => new
            {
                Code = g.Key,
                LatestValue = g.OrderByDescending(r => r.RecordedAt).First().ValueNumber
            })
            .ToListAsync(cancellationToken);

        var metricValues = recentMetrics
            .Where(m => m.LatestValue.HasValue)
            .ToDictionary(m => m.Code, m => m.LatestValue!.Value);

        // Calculate years added per model
        var breakdown = new List<LongevityBreakdownItem>();
        decimal totalYearsAdded = 0;

        foreach (var model in models)
        {
            var result = EvaluateModel(model, metricValues);
            if (result != null)
            {
                breakdown.Add(result);
                totalYearsAdded += result.YearsAdded;
            }
        }

        var adjustedLifeExpectancy = baseline + totalYearsAdded;

        // Calculate estimated death date based on date of birth
        DateTime? estimatedDeathDate = null;
        if (user.DateOfBirth.HasValue)
        {
            var dob = user.DateOfBirth.Value;
            var deathDate = dob.AddYears((int)Math.Round(adjustedLifeExpectancy));
            estimatedDeathDate = new DateTime(deathDate.Year, deathDate.Month, deathDate.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        // Determine confidence level
        var confidenceLevel = DetermineConfidenceLevel(metricValues.Count, models.Count);

        // Generate recommendations
        var recommendations = GenerateRecommendations(models, metricValues);

        return new LongevityEstimate(
            BaselineLifeExpectancy: baseline,
            EstimatedYearsAdded: Math.Round(totalYearsAdded, 1),
            AdjustedLifeExpectancy: Math.Round(adjustedLifeExpectancy, 1),
            EstimatedDeathDate: estimatedDeathDate,
            ConfidenceLevel: confidenceLevel,
            CalculatedAt: DateTime.UtcNow,
            Breakdown: breakdown,
            Recommendations: recommendations
        );
    }

    private LongevityBreakdownItem? EvaluateModel(Domain.Entities.LongevityModel model, Dictionary<string, decimal> metricValues)
    {
        try
        {
            var parameters = JsonSerializer.Deserialize<LongevityModelParameters>(model.Parameters);
            if (parameters == null)
                return null;

            // Check if we have the required metrics
            var inputMetrics = JsonSerializer.Deserialize<string[]>(model.InputMetrics) ?? Array.Empty<string>();
            var inputValues = new Dictionary<string, object?>();
            
            foreach (var metric in inputMetrics)
            {
                if (metricValues.TryGetValue(metric, out var value))
                    inputValues[metric] = value;
            }

            // If we don't have any input metrics, skip this model
            if (inputMetrics.Any() && !inputValues.Any())
                return null;

            decimal yearsAdded = 0;
            string? notes = null;

            switch (model.ModelType)
            {
                case Domain.Enums.LongevityModelType.Threshold:
                    (yearsAdded, notes) = EvaluateThresholdModel(parameters, metricValues);
                    break;
                case Domain.Enums.LongevityModelType.Range:
                    (yearsAdded, notes) = EvaluateRangeModel(parameters, metricValues);
                    break;
                case Domain.Enums.LongevityModelType.Linear:
                    (yearsAdded, notes) = EvaluateLinearModel(parameters, metricValues);
                    break;
                case Domain.Enums.LongevityModelType.Boolean:
                    (yearsAdded, notes) = EvaluateBooleanModel(parameters, metricValues);
                    break;
                default:
                    return null;
            }

            return new LongevityBreakdownItem(
                ModelCode: model.Code,
                ModelName: model.Name,
                YearsAdded: Math.Round(yearsAdded, 2),
                InputValues: inputValues,
                Notes: notes
            );
        }
        catch
        {
            return null;
        }
    }

    private (decimal yearsAdded, string? notes) EvaluateThresholdModel(
        LongevityModelParameters parameters, 
        Dictionary<string, decimal> metricValues)
    {
        if (string.IsNullOrEmpty(parameters.MetricCode) || 
            !metricValues.TryGetValue(parameters.MetricCode, out var value))
            return (0, null);

        if (parameters.Direction == "below" && value < parameters.Threshold)
            return (parameters.MaxYearsAdded, $"Value {value} is below threshold {parameters.Threshold}");
        
        if (parameters.Direction == "above" && value >= parameters.Threshold)
            return (parameters.MaxYearsAdded, $"Value {value} is at or above threshold {parameters.Threshold}");

        return (0, null);
    }

    private (decimal yearsAdded, string? notes) EvaluateRangeModel(
        LongevityModelParameters parameters, 
        Dictionary<string, decimal> metricValues)
    {
        if (string.IsNullOrEmpty(parameters.MetricCode) || 
            !metricValues.TryGetValue(parameters.MetricCode, out var value))
            return (0, null);

        if (parameters.OptimalMin.HasValue && parameters.OptimalMax.HasValue)
        {
            if (value >= parameters.OptimalMin && value <= parameters.OptimalMax)
                return (parameters.MaxYearsAdded, $"Value {value} is in optimal range ({parameters.OptimalMin}-{parameters.OptimalMax})");
        }

        return (0, null);
    }

    private (decimal yearsAdded, string? notes) EvaluateLinearModel(
        LongevityModelParameters parameters, 
        Dictionary<string, decimal> metricValues)
    {
        if (string.IsNullOrEmpty(parameters.MetricCode) || 
            !metricValues.TryGetValue(parameters.MetricCode, out var value))
            return (0, null);

        // Linear interpolation between min/max values
        if (!parameters.OptimalMin.HasValue || !parameters.OptimalMax.HasValue)
            return (0, null);

        var normalized = (value - parameters.OptimalMin.Value) / 
                        (parameters.OptimalMax.Value - parameters.OptimalMin.Value);
        normalized = Math.Clamp(normalized, 0, 1);

        var yearsAdded = normalized * parameters.MaxYearsAdded;
        return (yearsAdded, $"Linear scaling based on value {value}");
    }

    private (decimal yearsAdded, string? notes) EvaluateBooleanModel(
        LongevityModelParameters parameters, 
        Dictionary<string, decimal> metricValues)
    {
        // Boolean model checks if a flag metric is truthy (> 0)
        if (string.IsNullOrEmpty(parameters.MetricCode) || 
            !metricValues.TryGetValue(parameters.MetricCode, out var value))
            return (0, null);

        if (value > 0)
            return (parameters.MaxYearsAdded, $"Condition met ({parameters.MetricCode} > 0)");

        return (0, null);
    }

    private string DetermineConfidenceLevel(int metricCount, int modelCount)
    {
        if (metricCount == 0)
            return "low";
        if (metricCount < 3)
            return "low";
        if (metricCount < 5)
            return "moderate";
        return "high";
    }

    private List<LongevityRecommendation> GenerateRecommendations(
        List<Domain.Entities.LongevityModel> models, 
        Dictionary<string, decimal> metricValues)
    {
        var recommendations = new List<LongevityRecommendation>();

        foreach (var model in models)
        {
            try
            {
                var parameters = JsonSerializer.Deserialize<LongevityModelParameters>(model.Parameters);
                if (parameters == null)
                    continue;

                if (!metricValues.TryGetValue(parameters.MetricCode ?? "", out var currentValue))
                    continue;

                // Generate recommendation if not at optimal
                if (parameters.OptimalMin.HasValue && currentValue < parameters.OptimalMin)
                {
                    recommendations.Add(new LongevityRecommendation(
                        Area: model.Name,
                        Suggestion: $"Increase {parameters.MetricCode} to at least {parameters.OptimalMin} for additional longevity benefit",
                        PotentialGain: parameters.MaxYearsAdded
                    ));
                }
                else if (parameters.OptimalMax.HasValue && currentValue > parameters.OptimalMax)
                {
                    recommendations.Add(new LongevityRecommendation(
                        Area: model.Name,
                        Suggestion: $"Reduce {parameters.MetricCode} to below {parameters.OptimalMax} for additional longevity benefit",
                        PotentialGain: parameters.MaxYearsAdded
                    ));
                }
                else if (model.ModelType == Domain.Enums.LongevityModelType.Threshold && parameters.Direction == "above")
                {
                    if (currentValue < parameters.Threshold)
                    {
                        recommendations.Add(new LongevityRecommendation(
                            Area: model.Name,
                            Suggestion: $"Increase {parameters.MetricCode} to {parameters.Threshold}+ for additional {parameters.MaxYearsAdded} years",
                            PotentialGain: parameters.MaxYearsAdded
                        ));
                    }
                }
            }
            catch
            {
                // Skip invalid models
            }
        }

        return recommendations.Take(5).ToList();
    }
}

public class LongevityModelParameters
{
    [System.Text.Json.Serialization.JsonPropertyName("metricCode")]
    public string? MetricCode { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("threshold")]
    public decimal Threshold { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("direction")]
    public string? Direction { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("optimalMin")]
    public decimal? OptimalMin { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("optimalMax")]
    public decimal? OptimalMax { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("maxYearsAdded")]
    public decimal MaxYearsAdded { get; set; }
}
