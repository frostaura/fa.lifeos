using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Simulations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Simulations;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, EventListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetEventsQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<EventListResponse> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SimulationEvents
            .Include(e => e.Scenario)
            .Where(e => e.Scenario.UserId == request.UserId);

        if (request.ScenarioId.HasValue)
            query = query.Where(e => e.ScenarioId == request.ScenarioId.Value);

        var events = await query
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.TriggerDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new EventListResponse
        {
            Data = events.Select(e => new EventItemResponse
            {
                Id = e.Id,
                Type = "simulationEvent",
                Attributes = new EventAttributes
                {
                    ScenarioId = e.ScenarioId,
                    Name = e.Name,
                    Description = e.Description,
                    TriggerType = e.TriggerType.ToString().ToLowerInvariant(),
                    TriggerDate = e.TriggerDate,
                    TriggerAge = e.TriggerAge,
                    TriggerCondition = e.TriggerCondition,
                    EventType = e.EventType,
                    Currency = e.Currency,
                    AmountType = e.AmountType.ToString().ToLowerInvariant(),
                    AmountValue = e.AmountValue,
                    AmountFormula = e.AmountFormula,
                    AffectedAccountId = e.AffectedAccountId,
                    AppliesOnce = e.AppliesOnce,
                    RecurrenceFrequency = e.RecurrenceFrequency?.ToString().ToLowerInvariant(),
                    RecurrenceEndDate = e.RecurrenceEndDate,
                    SortOrder = e.SortOrder,
                    IsActive = e.IsActive
                }
            }).ToList(),
            Meta = new SimulationMeta { Timestamp = DateTime.UtcNow }
        };
    }
}
