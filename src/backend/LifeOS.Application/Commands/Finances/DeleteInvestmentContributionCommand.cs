using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public record DeleteInvestmentContributionCommand(
    Guid UserId,
    Guid ContributionId
) : IRequest<bool>;

public class DeleteInvestmentContributionCommandHandler 
    : IRequestHandler<DeleteInvestmentContributionCommand, bool>
{
    private readonly ILifeOSDbContext _db;

    public DeleteInvestmentContributionCommandHandler(ILifeOSDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(
        DeleteInvestmentContributionCommand command,
        CancellationToken cancellationToken)
    {
        var contribution = await _db.InvestmentContributions
            .FirstOrDefaultAsync(c => c.Id == command.ContributionId && c.UserId == command.UserId, cancellationToken);

        if (contribution == null)
            return false;

        _db.InvestmentContributions.Remove(contribution);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
