using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public record UpdateEventCommand(
    Guid UserId,
    Guid EventId,
    string? Name,
    string? Description,
    SimTriggerType? TriggerType,
    DateOnly? TriggerDate,
    short? TriggerAge,
    string? TriggerCondition,
    string? EventType,
    string? Currency,
    AmountType? AmountType,
    decimal? AmountValue,
    string? AmountFormula,
    Guid? AffectedAccountId,
    bool? AppliesOnce,
    PaymentFrequency? RecurrenceFrequency,
    DateOnly? RecurrenceEndDate,
    int? SortOrder,
    bool? IsActive
) : IRequest<bool>;
