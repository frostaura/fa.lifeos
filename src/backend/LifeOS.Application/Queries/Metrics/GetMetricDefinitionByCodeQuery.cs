using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Metrics;

public record GetMetricDefinitionByCodeQuery(string Code) : IRequest<MetricDefinitionDetailResponse?>;

public class GetMetricDefinitionByCodeQueryHandler : IRequestHandler<GetMetricDefinitionByCodeQuery, MetricDefinitionDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetMetricDefinitionByCodeQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MetricDefinitionDetailResponse?> Handle(GetMetricDefinitionByCodeQuery request, CancellationToken cancellationToken)
    {
        var definition = await _context.MetricDefinitions
            .Include(m => m.Dimension)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == request.Code, cancellationToken);

        if (definition == null)
            return null;

        return new MetricDefinitionDetailResponse
        {
            Data = new MetricDefinitionItemResponse
            {
                Id = definition.Id,
                Attributes = new MetricDefinitionAttributes
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Description = definition.Description,
                    DimensionId = definition.DimensionId,
                    DimensionCode = definition.Dimension?.Code,
                    Unit = definition.Unit,
                    ValueType = definition.ValueType.ToString().ToLowerInvariant(),
                    AggregationType = definition.AggregationType.ToString().ToLowerInvariant(),
                    EnumValues = definition.EnumValues,
                    MinValue = definition.MinValue,
                    MaxValue = definition.MaxValue,
                    TargetValue = definition.TargetValue,
                    TargetDirection = definition.TargetDirection.ToString(),
                    Icon = definition.Icon,
                    Tags = definition.Tags,
                    IsDerived = definition.IsDerived,
                    DerivationFormula = definition.DerivationFormula,
                    IsActive = definition.IsActive
                }
            }
        };
    }
}
