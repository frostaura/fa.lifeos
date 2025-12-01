using LifeOS.Application.DTOs.Finances;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record UpdateTaxProfileCommand(
    Guid UserId,
    Guid TaxProfileId,
    string? Name,
    List<TaxBracket>? Brackets,
    decimal? UifRate,
    decimal? UifCap,
    decimal? VatRate,
    bool? IsVatRegistered,
    TaxRebates? TaxRebates,
    bool? IsActive
) : IRequest<bool>;
