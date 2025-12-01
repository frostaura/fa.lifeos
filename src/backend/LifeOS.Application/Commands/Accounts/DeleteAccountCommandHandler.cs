using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Accounts;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteAccountCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

        if (account == null)
            return false;

        // Soft delete - preserve transaction history
        account.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
