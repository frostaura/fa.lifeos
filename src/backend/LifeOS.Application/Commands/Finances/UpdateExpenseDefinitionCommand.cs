using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record UpdateExpenseDefinitionCommand(
    Guid UserId,
    Guid ExpenseDefinitionId,
    string? Name,
    decimal? AmountValue,
    string? AmountFormula,
    PaymentFrequency? Frequency,
    DateOnly? StartDate,
    string? Category,
    bool? IsTaxDeductible,
    Guid? LinkedAccountId,
    bool? InflationAdjusted,
    bool? IsActive,
    EndConditionType? EndConditionType,
    Guid? EndConditionAccountId,
    DateOnly? EndDate,
    decimal? EndAmountThreshold,
    bool ClearEndConditionAccount = false
) : IRequest<bool>;
