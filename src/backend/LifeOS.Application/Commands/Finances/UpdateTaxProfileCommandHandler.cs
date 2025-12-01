using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Commands.Finances;

public class UpdateTaxProfileCommandHandler : IRequestHandler<UpdateTaxProfileCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateTaxProfileCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateTaxProfileCommand request, CancellationToken cancellationToken)
    {
        var taxProfile = await _context.TaxProfiles
            .FirstOrDefaultAsync(t => t.Id == request.TaxProfileId && t.UserId == request.UserId, cancellationToken);

        if (taxProfile == null)
            return false;

        if (!string.IsNullOrEmpty(request.Name))
            taxProfile.Name = request.Name;

        if (request.Brackets != null)
            taxProfile.Brackets = JsonSerializer.Serialize(request.Brackets);

        if (request.UifRate.HasValue)
            taxProfile.UifRate = request.UifRate.Value;

        if (request.UifCap.HasValue)
            taxProfile.UifCap = request.UifCap.Value;

        if (request.VatRate.HasValue)
            taxProfile.VatRate = request.VatRate.Value;

        if (request.IsVatRegistered.HasValue)
            taxProfile.IsVatRegistered = request.IsVatRegistered.Value;

        if (request.TaxRebates != null)
            taxProfile.TaxRebates = JsonSerializer.Serialize(request.TaxRebates);

        if (request.IsActive.HasValue)
            taxProfile.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
