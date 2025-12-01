using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record CreateIncomeSourceCommand(
    Guid UserId,
    string Name,
    string Currency,
    decimal BaseAmount,
    bool IsPreTax,
    Guid? TaxProfileId,
    PaymentFrequency PaymentFrequency,
    DateOnly? NextPaymentDate,
    decimal? AnnualIncreaseRate,
    string? EmployerName,
    string? Notes
) : IRequest<IncomeSourceDetailResponse>;
