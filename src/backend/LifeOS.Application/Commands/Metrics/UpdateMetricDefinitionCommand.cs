using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Metrics;

public record UpdateMetricDefinitionCommand(
    string Code,
    string? Name,
    string? Description,
    Guid? DimensionId,
    string? Unit,
    MetricValueType? ValueType,
    AggregationType? AggregationType,
    string[]? EnumValues,
    decimal? MinValue,
    decimal? MaxValue,
    decimal? TargetValue,
    string? Icon,
    string[]? Tags,
    bool? IsDerived,
    string? DerivationFormula,
    bool? IsActive
) : IRequest<bool>;

public class UpdateMetricDefinitionCommandHandler : IRequestHandler<UpdateMetricDefinitionCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateMetricDefinitionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateMetricDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _context.MetricDefinitions
            .FirstOrDefaultAsync(m => m.Code == request.Code, cancellationToken);

        if (definition == null)
            return false;

        if (!string.IsNullOrEmpty(request.Name))
            definition.Name = request.Name;

        if (request.Description != null)
            definition.Description = request.Description;

        if (request.DimensionId.HasValue)
            definition.DimensionId = request.DimensionId.Value;

        if (request.Unit != null)
            definition.Unit = request.Unit;

        if (request.ValueType.HasValue)
            definition.ValueType = request.ValueType.Value;

        if (request.AggregationType.HasValue)
            definition.AggregationType = request.AggregationType.Value;

        if (request.EnumValues != null)
            definition.EnumValues = request.EnumValues;

        if (request.MinValue.HasValue)
            definition.MinValue = request.MinValue.Value;

        if (request.MaxValue.HasValue)
            definition.MaxValue = request.MaxValue.Value;

        if (request.TargetValue.HasValue)
            definition.TargetValue = request.TargetValue.Value;

        if (request.Icon != null)
            definition.Icon = request.Icon;

        if (request.Tags != null)
            definition.Tags = request.Tags;

        if (request.IsDerived.HasValue)
            definition.IsDerived = request.IsDerived.Value;

        if (request.DerivationFormula != null)
            definition.DerivationFormula = request.DerivationFormula;

        if (request.IsActive.HasValue)
            definition.IsActive = request.IsActive.Value;

        definition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
