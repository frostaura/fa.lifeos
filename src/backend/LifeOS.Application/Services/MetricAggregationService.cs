using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Services;

public class MetricAggregationService : IMetricAggregationService
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<MetricAggregationService> _logger;

    public MetricAggregationService(
        ILifeOSDbContext context,
        ILogger<MetricAggregationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<decimal?> AggregateMetricAsync(
        string metricCode,
        Guid userId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(metricCode))
        {
            throw new ArgumentException("Metric code cannot be null or empty", nameof(metricCode));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time", nameof(endTime));
        }

        try
        {
            // Lookup metric definition
            var metricDefinition = await _context.MetricDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Code == metricCode && m.IsActive, cancellationToken);

            if (metricDefinition == null)
            {
                _logger.LogWarning("Metric definition not found for code: {MetricCode}", metricCode);
                return null;
            }

            // Query metric records in time window
            var metricRecords = await _context.MetricRecords
                .AsNoTracking()
                .Where(r => r.UserId == userId
                    && r.MetricCode == metricCode
                    && r.RecordedAt >= startTime
                    && r.RecordedAt <= endTime
                    && r.ValueNumber.HasValue)
                .Select(r => new { r.ValueNumber, r.RecordedAt })
                .ToListAsync(cancellationToken);

            // Return null if no records found
            if (metricRecords.Count == 0)
            {
                _logger.LogDebug("No metric records found for {MetricCode} between {Start} and {End}",
                    metricCode, startTime, endTime);
                return null;
            }

            // Apply aggregation strategy based on type
            var result = metricDefinition.AggregationType switch
            {
                AggregationType.Last => metricRecords
                    .OrderByDescending(r => r.RecordedAt)
                    .First()
                    .ValueNumber,

                AggregationType.Sum => metricRecords
                    .Sum(r => r.ValueNumber ?? 0),

                AggregationType.Average => metricRecords
                    .Average(r => r.ValueNumber ?? 0),

                AggregationType.Min => metricRecords
                    .Min(r => r.ValueNumber),

                AggregationType.Max => metricRecords
                    .Max(r => r.ValueNumber),

                AggregationType.Count => metricRecords.Count,

                _ => throw new NotSupportedException(
                    $"Aggregation type {metricDefinition.AggregationType} is not supported")
            };

            _logger.LogDebug("Aggregated {Count} records for {MetricCode} using {AggregationType}: {Result}",
                metricRecords.Count, metricCode, metricDefinition.AggregationType, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating metric {MetricCode} for user {UserId}",
                metricCode, userId);
            throw;
        }
    }
}
