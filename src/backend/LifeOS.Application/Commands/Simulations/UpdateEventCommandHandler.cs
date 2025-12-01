using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Simulations;

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateEventCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        // Find event and verify it belongs to user via scenario
        var evt = await _context.SimulationEvents
            .Include(e => e.Scenario)
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.Scenario.UserId == request.UserId, cancellationToken);

        if (evt == null)
            return false;

        if (request.Name != null)
            evt.Name = request.Name;

        if (request.Description != null)
            evt.Description = request.Description;

        if (request.TriggerType.HasValue)
            evt.TriggerType = request.TriggerType.Value;

        if (request.TriggerDate.HasValue)
            evt.TriggerDate = request.TriggerDate.Value;

        if (request.TriggerAge.HasValue)
            evt.TriggerAge = request.TriggerAge.Value;

        if (request.TriggerCondition != null)
            evt.TriggerCondition = request.TriggerCondition;

        if (request.EventType != null)
            evt.EventType = request.EventType;

        if (request.Currency != null)
            evt.Currency = request.Currency;

        if (request.AmountType.HasValue)
            evt.AmountType = request.AmountType.Value;

        if (request.AmountValue.HasValue)
            evt.AmountValue = request.AmountValue.Value;

        if (request.AmountFormula != null)
            evt.AmountFormula = request.AmountFormula;

        if (request.AffectedAccountId.HasValue)
            evt.AffectedAccountId = request.AffectedAccountId.Value;

        if (request.AppliesOnce.HasValue)
            evt.AppliesOnce = request.AppliesOnce.Value;

        if (request.RecurrenceFrequency.HasValue)
            evt.RecurrenceFrequency = request.RecurrenceFrequency.Value;

        if (request.RecurrenceEndDate.HasValue)
            evt.RecurrenceEndDate = request.RecurrenceEndDate.Value;

        if (request.SortOrder.HasValue)
            evt.SortOrder = request.SortOrder.Value;

        if (request.IsActive.HasValue)
            evt.IsActive = request.IsActive.Value;

        evt.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
