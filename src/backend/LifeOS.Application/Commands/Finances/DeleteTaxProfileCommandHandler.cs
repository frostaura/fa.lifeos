using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public class DeleteTaxProfileCommandHandler : IRequestHandler<DeleteTaxProfileCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteTaxProfileCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteTaxProfileCommand request, CancellationToken cancellationToken)
    {
        var taxProfile = await _context.TaxProfiles
            .FirstOrDefaultAsync(t => t.Id == request.TaxProfileId && t.UserId == request.UserId, cancellationToken);

        if (taxProfile == null)
            return false;

        _context.TaxProfiles.Remove(taxProfile);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
