using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Simulations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Simulations;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetEventByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<EventDetailResponse?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var evt = await _context.SimulationEvents
            .Include(e => e.Scenario)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.Scenario.UserId == request.UserId, cancellationToken);

        if (evt == null)
            return null;

        return new EventDetailResponse
        {
            Data = new EventItemResponse
            {
                Id = evt.Id,
                Type = "simulationEvent",
                Attributes = new EventAttributes
                {
                    ScenarioId = evt.ScenarioId,
                    Name = evt.Name,
                    Description = evt.Description,
                    TriggerType = evt.TriggerType.ToString().ToLowerInvariant(),
                    TriggerDate = evt.TriggerDate,
                    TriggerAge = evt.TriggerAge,
                    TriggerCondition = evt.TriggerCondition,
                    EventType = evt.EventType,
                    Currency = evt.Currency,
                    AmountType = evt.AmountType.ToString().ToLowerInvariant(),
                    AmountValue = evt.AmountValue,
                    AmountFormula = evt.AmountFormula,
                    AffectedAccountId = evt.AffectedAccountId,
                    AppliesOnce = evt.AppliesOnce,
                    RecurrenceFrequency = evt.RecurrenceFrequency?.ToString().ToLowerInvariant(),
                    RecurrenceEndDate = evt.RecurrenceEndDate,
                    SortOrder = evt.SortOrder,
                    IsActive = evt.IsActive
                }
            },
            Meta = new SimulationMeta { Timestamp = DateTime.UtcNow }
        };
    }
}
