using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Metrics;

public record GetMetricDefinitionsQuery(
    Guid? DimensionId = null,
    string[]? Tags = null,
    bool? IsActive = true
) : IRequest<MetricDefinitionListResponse>;

public class GetMetricDefinitionsQueryHandler : IRequestHandler<GetMetricDefinitionsQuery, MetricDefinitionListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetMetricDefinitionsQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MetricDefinitionListResponse> Handle(GetMetricDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MetricDefinitions
            .Include(m => m.Dimension)
            .AsQueryable();

        if (request.DimensionId.HasValue)
        {
            query = query.Where(m => m.DimensionId == request.DimensionId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(m => m.IsActive == request.IsActive.Value);
        }

        if (request.Tags != null && request.Tags.Length > 0)
        {
            query = query.Where(m => m.Tags != null && m.Tags.Any(t => request.Tags.Contains(t)));
        }

        var definitions = await query
            .OrderBy(m => m.Dimension != null ? m.Dimension.SortOrder : 999)
            .ThenBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get latest records for each metric code
        var metricCodes = definitions.Select(d => d.Code).ToList();
        var latestRecords = await _context.MetricRecords
            .Where(r => metricCodes.Contains(r.MetricCode))
            .GroupBy(r => r.MetricCode)
            .Select(g => new
            {
                Code = g.Key,
                LatestValue = g.OrderByDescending(r => r.RecordedAt).First().ValueNumber,
                LatestRecordedAt = g.OrderByDescending(r => r.RecordedAt).First().RecordedAt
            })
            .ToListAsync(cancellationToken);

        var latestRecordLookup = latestRecords.ToDictionary(r => r.Code);

        // Build response and order by latest recorded first
        var results = definitions.Select(m => {
            latestRecordLookup.TryGetValue(m.Code, out var latestRecord);
            return new MetricDefinitionItemResponse
            {
                Id = m.Id,
                Attributes = new MetricDefinitionAttributes
                {
                    Code = m.Code,
                    Name = m.Name,
                    Description = m.Description,
                    DimensionId = m.DimensionId,
                    DimensionCode = m.Dimension?.Code,
                    Unit = m.Unit,
                    ValueType = m.ValueType.ToString().ToLowerInvariant(),
                    AggregationType = m.AggregationType.ToString().ToLowerInvariant(),
                    MinValue = m.MinValue,
                    MaxValue = m.MaxValue,
                    TargetValue = m.TargetValue,
                    TargetDirection = m.TargetDirection.ToString(),
                    Icon = m.Icon,
                    Tags = m.Tags,
                    IsDerived = m.IsDerived,
                    IsActive = m.IsActive,
                    LatestValue = latestRecord?.LatestValue,
                    LatestRecordedAt = latestRecord?.LatestRecordedAt
                }
            };
        })
        .OrderByDescending(r => r.Attributes.LatestRecordedAt ?? DateTime.MinValue)
        .ThenBy(r => r.Attributes.Name)
        .ToList();

        return new MetricDefinitionListResponse
        {
            Data = results
        };
    }
}
