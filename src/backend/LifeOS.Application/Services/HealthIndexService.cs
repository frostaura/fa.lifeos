using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

/// <summary>
/// v1.2: Health Index calculation service
/// Calculates health score (0-100) from health metrics
/// </summary>
public interface IHealthIndexService
{
    Task<HealthIndexSnapshot> CalculateHealthIndexAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class HealthIndexService : IHealthIndexService
{
    private readonly ILifeOSDbContext _context;

    public HealthIndexService(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<HealthIndexSnapshot> CalculateHealthIndexAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Get health-related metric definitions with targets
        var healthMetrics = await _context.MetricDefinitions
            .Where(m => m.IsActive && m.TargetValue.HasValue)
            .ToListAsync(cancellationToken);

        if (!healthMetrics.Any())
        {
            return new HealthIndexSnapshot
            {
                UserId = userId,
                Score = 50m,
                Components = "[]"
            };
        }

        var components = new List<object>();
        decimal totalScore = 0;
        decimal totalWeight = 0;

        foreach (var metric in healthMetrics)
        {
            // Get latest value for this metric
            var latestRecord = await _context.MetricRecords
                .Where(r => r.UserId == userId && r.MetricCode == metric.Code)
                .OrderByDescending(r => r.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestRecord?.ValueNumber == null)
                continue;

            var value = latestRecord.ValueNumber.Value;
            var target = metric.TargetValue.Value;
            
            // Calculate score for this metric (0-100)
            decimal metricScore = CalculateMetricScore(value, target, metric.MinValue, metric.MaxValue, metric.TargetDirection);
            
            // Simple equal weighting for now
            decimal weight = 1.0m / healthMetrics.Count;
            
            components.Add(new
            {
                metricCode = metric.Code,
                score = Math.Round(metricScore, 2),
                weight = Math.Round(weight, 4),
                currentValue = Math.Round(value, 2),
                targetValue = Math.Round(target, 2)
            });

            totalScore += metricScore * weight;
            totalWeight += weight;
        }

        var finalScore = totalWeight > 0 ? totalScore / totalWeight : 50m;

        var snapshot = new HealthIndexSnapshot
        {
            UserId = userId,
            Score = Math.Round(finalScore, 2),
            Components = System.Text.Json.JsonSerializer.Serialize(components)
        };

        _context.HealthIndexSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        return snapshot;
    }

    private decimal CalculateMetricScore(decimal value, decimal target, decimal? min, decimal? max, Domain.Enums.TargetDirection direction)
    {
        // Simple scoring logic:
        // - If value equals target: 100
        // - If value is within 10% of target: 90+
        // - Linear interpolation otherwise

        if (value == target)
            return 100m;

        decimal tolerance = Math.Abs(target * 0.1m);

        if (direction == Domain.Enums.TargetDirection.AtOrAbove)
        {
            if (value >= target)
                return 100m;
            if (value >= target - tolerance)
                return 90m + (10m * (value - (target - tolerance)) / tolerance);
            if (min.HasValue)
            {
                var range = target - min.Value;
                if (range > 0)
                    return Math.Max(0, 90m * (value - min.Value) / range);
            }
            return Math.Max(0, 50m * value / target);
        }
        else // AtOrBelow
        {
            if (value <= target)
                return 100m;
            if (value <= target + tolerance)
                return 90m + (10m * ((target + tolerance) - value) / tolerance);
            if (max.HasValue)
            {
                var range = max.Value - target;
                if (range > 0)
                    return Math.Max(0, 90m * (max.Value - value) / range);
            }
            return Math.Max(0, 50m * target / value);
        }
    }
}
