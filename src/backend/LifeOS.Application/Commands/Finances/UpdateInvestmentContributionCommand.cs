using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public record UpdateInvestmentContributionCommand(
    Guid UserId,
    Guid ContributionId,
    UpdateInvestmentContributionRequest Request
) : IRequest<InvestmentContributionDto?>;

public class UpdateInvestmentContributionCommandHandler 
    : IRequestHandler<UpdateInvestmentContributionCommand, InvestmentContributionDto?>
{
    private readonly ILifeOSDbContext _db;

    public UpdateInvestmentContributionCommandHandler(ILifeOSDbContext db)
    {
        _db = db;
    }

    public async Task<InvestmentContributionDto?> Handle(
        UpdateInvestmentContributionCommand command,
        CancellationToken cancellationToken)
    {
        var contribution = await _db.InvestmentContributions
            .Include(c => c.TargetAccount)
            .FirstOrDefaultAsync(c => c.Id == command.ContributionId && c.UserId == command.UserId, cancellationToken);

        if (contribution == null)
            return null;

        if (command.Request.Name != null)
            contribution.Name = command.Request.Name;
        if (command.Request.Amount.HasValue)
            contribution.Amount = command.Request.Amount.Value;
        if (command.Request.Frequency.HasValue)
            contribution.Frequency = command.Request.Frequency.Value;
        if (command.Request.TargetAccountId.HasValue)
            contribution.TargetAccountId = command.Request.TargetAccountId;
        if (command.Request.Category != null)
            contribution.Category = command.Request.Category;
        if (command.Request.AnnualIncreaseRate.HasValue)
            contribution.AnnualIncreaseRate = command.Request.AnnualIncreaseRate;
        if (command.Request.Notes != null)
            contribution.Notes = command.Request.Notes;
        if (command.Request.IsActive.HasValue)
            contribution.IsActive = command.Request.IsActive.Value;

        contribution.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new InvestmentContributionDto
        {
            Id = contribution.Id,
            Name = contribution.Name,
            Currency = contribution.Currency,
            Amount = contribution.Amount,
            Frequency = contribution.Frequency,
            TargetAccountId = contribution.TargetAccountId,
            TargetAccountName = contribution.TargetAccount?.Name,
            Category = contribution.Category,
            AnnualIncreaseRate = contribution.AnnualIncreaseRate,
            Notes = contribution.Notes,
            IsActive = contribution.IsActive,
            CreatedAt = contribution.CreatedAt
        };
    }
}
