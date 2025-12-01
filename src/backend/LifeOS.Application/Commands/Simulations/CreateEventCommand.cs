using LifeOS.Application.DTOs.Simulations;
using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public record CreateEventCommand(
    Guid UserId,
    Guid ScenarioId,
    string Name,
    string? Description,
    SimTriggerType TriggerType,
    DateOnly? TriggerDate,
    short? TriggerAge,
    string? TriggerCondition,
    string EventType,
    string? Currency,
    AmountType AmountType,
    decimal? AmountValue,
    string? AmountFormula,
    Guid? AffectedAccountId,
    bool AppliesOnce,
    PaymentFrequency? RecurrenceFrequency,
    DateOnly? RecurrenceEndDate,
    int SortOrder
) : IRequest<EventDetailResponse>;
