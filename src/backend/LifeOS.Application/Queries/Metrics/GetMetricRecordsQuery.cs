using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Metrics;

public record GetMetricRecordsQuery(
    Guid UserId,
    string? Code,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20
) : IRequest<MetricRecordListResponse>;

public class GetMetricRecordsQueryHandler : IRequestHandler<GetMetricRecordsQuery, MetricRecordListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetMetricRecordsQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MetricRecordListResponse> Handle(GetMetricRecordsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MetricRecords
            .Where(r => r.UserId == request.UserId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Code))
        {
            query = query.Where(r => r.MetricCode == request.Code);
        }

        if (request.From.HasValue)
        {
            query = query.Where(r => r.RecordedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(r => r.RecordedAt <= request.To.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var pageSize = Math.Min(request.PageSize, 100);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var records = await query
            .OrderByDescending(r => r.RecordedAt)
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new MetricRecordListResponse
        {
            Data = records.Select(r => new MetricRecordItemResponse
            {
                Id = r.Id,
                Attributes = new MetricRecordAttributes
                {
                    MetricCode = r.MetricCode,
                    ValueNumber = r.ValueNumber,
                    ValueBoolean = r.ValueBoolean,
                    ValueString = r.ValueString,
                    RecordedAt = r.RecordedAt,
                    Source = r.Source,
                    Notes = r.Notes,
                    Metadata = r.Metadata
                }
            }).ToList(),
            Meta = new MetricRecordListMeta
            {
                Page = request.Page,
                PerPage = pageSize,
                Total = total,
                TotalPages = totalPages,
                Timestamp = DateTime.UtcNow
            }
        };
    }
}
