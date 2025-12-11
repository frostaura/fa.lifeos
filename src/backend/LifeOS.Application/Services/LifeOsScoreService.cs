using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

/// <summary>
/// v1.2: Master LifeOS Score calculation service
/// Aggregates all scoring systems into one comprehensive score
/// </summary>
public interface ILifeOsScoreService
{
    Task<LifeOsScoreSnapshot> CalculateLifeOsScoreAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class LifeOsScoreService : ILifeOsScoreService
{
    private readonly ILifeOSDbContext _context;
    private readonly IHealthIndexService _healthIndexService;
    private readonly IAdherenceService _adherenceService;
    private readonly IWealthHealthService _wealthHealthService;

    public LifeOsScoreService(
        ILifeOSDbContext context,
        IHealthIndexService healthIndexService,
        IAdherenceService adherenceService,
        IWealthHealthService wealthHealthService)
    {
        _context = context;
        _healthIndexService = healthIndexService;
        _adherenceService = adherenceService;
        _wealthHealthService = wealthHealthService;
    }

    public async Task<LifeOsScoreSnapshot> CalculateLifeOsScoreAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Calculate component scores
        var healthIndex = await _healthIndexService.CalculateHealthIndexAsync(userId, cancellationToken);
        var adherenceIndex = await _adherenceService.CalculateAdherenceAsync(userId, 7, cancellationToken);
        var wealthHealth = await _wealthHealthService.CalculateWealthHealthAsync(userId, cancellationToken);
        
        // Get latest longevity snapshot
        var longevitySnapshot = await _context.LongevitySnapshots
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CalculatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var longevityYearsAdded = longevitySnapshot?.EstimatedYearsAdded ?? 0;

        // Calculate per-dimension scores
        var dimensions = await _context.Dimensions
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);

        var dimensionScores = new List<object>();
        foreach (var dimension in dimensions)
        {
            var dimScore = await CalculateDimensionScore(userId, dimension.Id, cancellationToken);
            dimensionScores.Add(new
            {
                dimensionCode = dimension.Code,
                score = Math.Round(dimScore, 2),
                weight = dimension.DefaultWeight
            });
        }

        // Calculate overall LifeOS Score
        // Weighted combination: 40% Health, 30% Adherence, 30% Wealth
        var lifeScore = (
            healthIndex.Score * 0.40m +
            adherenceIndex.Score * 0.30m +
            wealthHealth.Score * 0.30m
        );

        var snapshot = new LifeOsScoreSnapshot
        {
            UserId = userId,
            LifeScore = Math.Round(lifeScore, 2),
            HealthIndex = healthIndex.Score,
            AdherenceIndex = adherenceIndex.Score,
            WealthHealthScore = wealthHealth.Score,
            LongevityYearsAdded = longevityYearsAdded,
            DimensionScores = System.Text.Json.JsonSerializer.Serialize(dimensionScores)
        };

        _context.LifeOsScoreSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        return snapshot;
    }

    private async Task<decimal> CalculateDimensionScore(Guid userId, Guid dimensionId, CancellationToken cancellationToken)
    {
        // Get all metrics for this dimension
        var metrics = await _context.MetricDefinitions
            .Where(m => m.DimensionId == dimensionId && m.IsActive)
            .ToListAsync(cancellationToken);

        if (!metrics.Any())
            return 50m;

        decimal totalScore = 0;
        int count = 0;

        foreach (var metric in metrics)
        {
            var latestRecord = await _context.MetricRecords
                .Where(r => r.UserId == userId && r.MetricCode == metric.Code)
                .OrderByDescending(r => r.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestRecord?.ValueNumber != null && metric.TargetValue.HasValue)
            {
                var score = CalculateMetricScore(latestRecord.ValueNumber.Value, metric.TargetValue.Value);
                totalScore += score;
                count++;
            }
        }

        return count > 0 ? totalScore / count : 50m;
    }

    private decimal CalculateMetricScore(decimal value, decimal target)
    {
        if (value == target) return 100m;
        
        var percentDiff = Math.Abs((value - target) / target);
        
        if (percentDiff <= 0.1m) return 90m + 10m * (0.1m - percentDiff) / 0.1m;
        if (percentDiff <= 0.25m) return 70m + 20m * (0.25m - percentDiff) / 0.15m;
        if (percentDiff <= 0.5m) return 50m + 20m * (0.5m - percentDiff) / 0.25m;
        
        return Math.Max(0, 50m - 50m * (percentDiff - 0.5m));
    }
}
