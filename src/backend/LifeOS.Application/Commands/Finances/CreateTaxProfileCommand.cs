using LifeOS.Application.DTOs.Finances;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record CreateTaxProfileCommand(
    Guid UserId,
    string Name,
    int TaxYear,
    string CountryCode,
    List<TaxBracket>? Brackets,
    decimal? UifRate,
    decimal? UifCap,
    decimal? VatRate,
    bool IsVatRegistered,
    TaxRebates? TaxRebates
) : IRequest<TaxProfileDetailResponse>;
