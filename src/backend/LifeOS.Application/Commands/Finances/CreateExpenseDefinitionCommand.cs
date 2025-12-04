using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record CreateExpenseDefinitionCommand(
    Guid UserId,
    string Name,
    string Currency,
    AmountType AmountType,
    decimal? AmountValue,
    string? AmountFormula,
    PaymentFrequency Frequency,
    DateOnly? StartDate,
    string Category,
    bool IsTaxDeductible,
    Guid? LinkedAccountId,
    bool InflationAdjusted,
    EndConditionType EndConditionType,
    Guid? EndConditionAccountId,
    DateOnly? EndDate,
    decimal? EndAmountThreshold
) : IRequest<ExpenseDefinitionDetailResponse>;
