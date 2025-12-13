using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record UpdateIncomeSourceCommand(
    Guid UserId,
    Guid IncomeSourceId,
    string? Name,
    decimal? BaseAmount,
    Guid? TaxProfileId,
    bool ClearTaxProfile,
    PaymentFrequency? PaymentFrequency,
    DateOnly? NextPaymentDate,
    decimal? AnnualIncreaseRate,
    string? EmployerName,
    string? Notes,
    bool? IsActive,
    Guid? TargetAccountId,
    EndConditionType? EndConditionType,
    Guid? EndConditionAccountId,
    DateOnly? EndDate,
    decimal? EndAmountThreshold
) : IRequest<bool>;
