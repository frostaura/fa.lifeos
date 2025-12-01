using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace LifeOS.Application.Commands.Finances;

public class CreateTaxProfileCommandHandler : IRequestHandler<CreateTaxProfileCommand, TaxProfileDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateTaxProfileCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TaxProfileDetailResponse> Handle(CreateTaxProfileCommand request, CancellationToken cancellationToken)
    {
        var taxProfile = new TaxProfile
        {
            UserId = request.UserId,
            Name = request.Name,
            TaxYear = request.TaxYear,
            CountryCode = request.CountryCode.ToUpperInvariant(),
            Brackets = request.Brackets != null 
                ? JsonSerializer.Serialize(request.Brackets) 
                : "[]",
            UifRate = request.UifRate,
            UifCap = request.UifCap,
            VatRate = request.VatRate,
            IsVatRegistered = request.IsVatRegistered,
            TaxRebates = request.TaxRebates != null 
                ? JsonSerializer.Serialize(request.TaxRebates) 
                : null,
            IsActive = true
        };

        _context.TaxProfiles.Add(taxProfile);
        await _context.SaveChangesAsync(cancellationToken);

        return new TaxProfileDetailResponse
        {
            Data = new TaxProfileItemResponse
            {
                Id = taxProfile.Id,
                Type = "taxProfile",
                Attributes = new TaxProfileAttributes
                {
                    Name = taxProfile.Name,
                    TaxYear = taxProfile.TaxYear,
                    CountryCode = taxProfile.CountryCode,
                    Brackets = request.Brackets,
                    UifRate = taxProfile.UifRate,
                    UifCap = taxProfile.UifCap,
                    VatRate = taxProfile.VatRate,
                    IsVatRegistered = taxProfile.IsVatRegistered,
                    TaxRebates = request.TaxRebates,
                    IsActive = taxProfile.IsActive
                }
            }
        };
    }
}
