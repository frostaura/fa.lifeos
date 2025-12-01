using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Simulations;
using LifeOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Simulations;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateEventCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<EventDetailResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // Verify scenario belongs to user
        var scenario = await _context.SimulationScenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.UserId == request.UserId, cancellationToken);

        if (scenario == null)
            throw new InvalidOperationException("Scenario not found");

        var simulationEvent = new SimulationEvent
        {
            ScenarioId = request.ScenarioId,
            Name = request.Name,
            Description = request.Description,
            TriggerType = request.TriggerType,
            TriggerDate = request.TriggerDate,
            TriggerAge = request.TriggerAge,
            TriggerCondition = request.TriggerCondition,
            EventType = request.EventType,
            Currency = request.Currency,
            AmountType = request.AmountType,
            AmountValue = request.AmountValue,
            AmountFormula = request.AmountFormula,
            AffectedAccountId = request.AffectedAccountId,
            AppliesOnce = request.AppliesOnce,
            RecurrenceFrequency = request.RecurrenceFrequency,
            RecurrenceEndDate = request.RecurrenceEndDate,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.SimulationEvents.Add(simulationEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return new EventDetailResponse
        {
            Data = new EventItemResponse
            {
                Id = simulationEvent.Id,
                Type = "simulationEvent",
                Attributes = new EventAttributes
                {
                    ScenarioId = simulationEvent.ScenarioId,
                    Name = simulationEvent.Name,
                    Description = simulationEvent.Description,
                    TriggerType = simulationEvent.TriggerType.ToString().ToLowerInvariant(),
                    TriggerDate = simulationEvent.TriggerDate,
                    TriggerAge = simulationEvent.TriggerAge,
                    TriggerCondition = simulationEvent.TriggerCondition,
                    EventType = simulationEvent.EventType,
                    Currency = simulationEvent.Currency,
                    AmountType = simulationEvent.AmountType.ToString().ToLowerInvariant(),
                    AmountValue = simulationEvent.AmountValue,
                    AmountFormula = simulationEvent.AmountFormula,
                    AffectedAccountId = simulationEvent.AffectedAccountId,
                    AppliesOnce = simulationEvent.AppliesOnce,
                    RecurrenceFrequency = simulationEvent.RecurrenceFrequency?.ToString().ToLowerInvariant(),
                    RecurrenceEndDate = simulationEvent.RecurrenceEndDate,
                    SortOrder = simulationEvent.SortOrder,
                    IsActive = simulationEvent.IsActive
                }
            },
            Meta = new SimulationMeta { Timestamp = DateTime.UtcNow }
        };
    }
}
