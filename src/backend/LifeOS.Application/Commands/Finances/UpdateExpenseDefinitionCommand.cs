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
    string? Category,
    bool? IsTaxDeductible,
    Guid? LinkedAccountId,
    bool? InflationAdjusted,
    bool? IsActive
) : IRequest<bool>;
