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
    string Category,
    bool IsTaxDeductible,
    Guid? LinkedAccountId,
    bool InflationAdjusted
) : IRequest<ExpenseDefinitionDetailResponse>;
