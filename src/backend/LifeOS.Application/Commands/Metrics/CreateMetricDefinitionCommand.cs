using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Metrics;

public record CreateMetricDefinitionCommand(
    string Code,
    string Name,
    string? Description,
    Guid? DimensionId,
    string? Unit,
    MetricValueType ValueType,
    AggregationType AggregationType,
    string[]? EnumValues,
    decimal? MinValue,
    decimal? MaxValue,
    decimal? TargetValue,
    TargetDirection TargetDirection,
    string? Icon,
    string[]? Tags,
    bool IsDerived,
    string? DerivationFormula,
    bool IsActive
) : IRequest<MetricDefinitionDetailResponse>;

public class CreateMetricDefinitionCommandHandler : IRequestHandler<CreateMetricDefinitionCommand, MetricDefinitionDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateMetricDefinitionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MetricDefinitionDetailResponse> Handle(CreateMetricDefinitionCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate code
        var existingCode = await _context.MetricDefinitions
            .AnyAsync(m => m.Code == request.Code, cancellationToken);

        if (existingCode)
        {
            throw new InvalidOperationException($"Metric definition with code '{request.Code}' already exists");
        }

        var definition = new MetricDefinition
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            DimensionId = request.DimensionId,
            Unit = request.Unit,
            ValueType = request.ValueType,
            AggregationType = request.AggregationType,
            EnumValues = request.EnumValues,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            TargetValue = request.TargetValue,
            TargetDirection = request.TargetDirection,
            Icon = request.Icon,
            Tags = request.Tags,
            IsDerived = request.IsDerived,
            DerivationFormula = request.DerivationFormula,
            IsActive = request.IsActive
        };

        _context.MetricDefinitions.Add(definition);
        await _context.SaveChangesAsync(cancellationToken);

        // Load dimension for response
        var savedDefinition = await _context.MetricDefinitions
            .Include(m => m.Dimension)
            .FirstAsync(m => m.Id == definition.Id, cancellationToken);

        return new MetricDefinitionDetailResponse
        {
            Data = new MetricDefinitionItemResponse
            {
                Id = savedDefinition.Id,
                Attributes = new MetricDefinitionAttributes
                {
                    Code = savedDefinition.Code,
                    Name = savedDefinition.Name,
                    Description = savedDefinition.Description,
                    DimensionId = savedDefinition.DimensionId,
                    DimensionCode = savedDefinition.Dimension?.Code,
                    Unit = savedDefinition.Unit,
                    ValueType = savedDefinition.ValueType.ToString().ToLowerInvariant(),
                    AggregationType = savedDefinition.AggregationType.ToString().ToLowerInvariant(),
                    EnumValues = savedDefinition.EnumValues,
                    MinValue = savedDefinition.MinValue,
                    MaxValue = savedDefinition.MaxValue,
                    TargetValue = savedDefinition.TargetValue,
                    TargetDirection = savedDefinition.TargetDirection.ToString(),
                    Icon = savedDefinition.Icon,
                    Tags = savedDefinition.Tags,
                    IsDerived = savedDefinition.IsDerived,
                    DerivationFormula = savedDefinition.DerivationFormula,
                    IsActive = savedDefinition.IsActive
                }
            }
        };
    }
}
