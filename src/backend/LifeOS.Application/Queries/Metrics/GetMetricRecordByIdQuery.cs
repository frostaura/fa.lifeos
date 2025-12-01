using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Metrics;

public record GetMetricRecordByIdQuery(Guid UserId, Guid RecordId) : IRequest<MetricRecordDetailResponse?>;

public class GetMetricRecordByIdQueryHandler : IRequestHandler<GetMetricRecordByIdQuery, MetricRecordDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetMetricRecordByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MetricRecordDetailResponse?> Handle(GetMetricRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await _context.MetricRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RecordId && r.UserId == request.UserId, cancellationToken);

        if (record == null)
            return null;

        return new MetricRecordDetailResponse
        {
            Data = new MetricRecordItemResponse
            {
                Id = record.Id,
                Attributes = new MetricRecordAttributes
                {
                    MetricCode = record.MetricCode,
                    ValueNumber = record.ValueNumber,
                    ValueBoolean = record.ValueBoolean,
                    ValueString = record.ValueString,
                    RecordedAt = record.RecordedAt,
                    Source = record.Source,
                    Notes = record.Notes,
                    Metadata = record.Metadata
                }
            }
        };
    }
}
