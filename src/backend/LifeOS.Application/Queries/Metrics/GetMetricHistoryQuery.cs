using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Metrics;

public record GetMetricHistoryQuery(
    Guid UserId,
    string[] Codes,
    DateTime? From,
    DateTime? To,
    string Granularity = "raw",
    int Limit = 100
) : IRequest<MetricHistoryResponse>;

public class GetMetricHistoryQueryHandler : IRequestHandler<GetMetricHistoryQuery, MetricHistoryResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetMetricHistoryQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MetricHistoryResponse> Handle(GetMetricHistoryQuery request, CancellationToken cancellationToken)
    {
        var from = request.From.HasValue 
            ? DateTime.SpecifyKind(request.From.Value, DateTimeKind.Utc) 
            : DateTime.UtcNow.AddDays(-30);
        var to = request.To.HasValue 
            ? DateTime.SpecifyKind(request.To.Value, DateTimeKind.Utc) 
            : DateTime.UtcNow;

        var result = new Dictionary<string, MetricHistoryData>();

        // Get metric definitions for aggregation type and target values
        var metricDefs = await _context.MetricDefinitions
            .Where(m => request.Codes.Contains(m.Code))
            .ToDictionaryAsync(m => m.Code, m => new { m.AggregationType, m.TargetValue }, cancellationToken);

        foreach (var code in request.Codes)
        {
            var query = _context.MetricRecords
                .Where(r => r.UserId == request.UserId 
                    && r.MetricCode == code 
                    && r.RecordedAt >= from 
                    && r.RecordedAt <= to)
                .AsNoTracking();

            List<MetricHistoryPoint> points;
            if (request.Granularity == "raw")
            {
                points = await query
                    .OrderByDescending(r => r.RecordedAt)
                    .Take(request.Limit)
                    .Select(r => new MetricHistoryPoint
                    {
                        Timestamp = r.RecordedAt,
                        Value = r.ValueNumber ?? 0,
                        Source = r.Source
                    })
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // Aggregate by granularity
                var aggregationType = metricDefs.TryGetValue(code, out var def) ? def.AggregationType : AggregationType.Last;
                points = await AggregateByGranularity(query, request.Granularity, aggregationType, request.Limit, cancellationToken);
            }

            result[code] = new MetricHistoryData
            {
                Points = points,
                TargetValue = metricDefs.TryGetValue(code, out var metricDef) ? metricDef.TargetValue : null
            };
        }

        return new MetricHistoryResponse
        {
            Data = result,
            Meta = new MetricHistoryMeta
            {
                From = from,
                To = to,
                Granularity = request.Granularity,
                MetricsReturned = result.Keys.ToList()
            }
        };
    }

    private async Task<List<MetricHistoryPoint>> AggregateByGranularity(
        IQueryable<Domain.Entities.MetricRecord> query,
        string granularity,
        AggregationType aggregationType,
        int limit,
        CancellationToken cancellationToken)
    {
        // Group by date part based on granularity
        var records = await query.ToListAsync(cancellationToken);
        
        var grouped = granularity switch
        {
            "hourly" => records.GroupBy(r => new DateTime(r.RecordedAt.Year, r.RecordedAt.Month, r.RecordedAt.Day, r.RecordedAt.Hour, 0, 0, DateTimeKind.Utc)),
            "daily" => records.GroupBy(r => DateTime.SpecifyKind(r.RecordedAt.Date, DateTimeKind.Utc)),
            "weekly" => records.GroupBy(r => DateTime.SpecifyKind(r.RecordedAt.Date.AddDays(-(int)r.RecordedAt.DayOfWeek), DateTimeKind.Utc)),
            "monthly" => records.GroupBy(r => new DateTime(r.RecordedAt.Year, r.RecordedAt.Month, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => records.GroupBy(r => r.RecordedAt)
        };

        var aggregated = grouped.Select(g => new MetricHistoryPoint
        {
            Timestamp = g.Key,
            Value = aggregationType switch
            {
                AggregationType.Sum => g.Sum(r => r.ValueNumber ?? 0),
                AggregationType.Average => g.Average(r => r.ValueNumber ?? 0),
                AggregationType.Min => g.Min(r => r.ValueNumber ?? 0),
                AggregationType.Max => g.Max(r => r.ValueNumber ?? 0),
                AggregationType.Count => g.Count(),
                AggregationType.Last => g.OrderByDescending(r => r.RecordedAt).First().ValueNumber ?? 0,
                _ => g.Average(r => r.ValueNumber ?? 0)
            },
            Aggregation = aggregationType.ToString().ToLowerInvariant()
        })
        .OrderByDescending(p => p.Timestamp)
        .Take(limit)
        .ToList();

        return aggregated;
    }
}
