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
            .Include(c => c.SourceAccount)
            .Include(c => c.EndConditionAccount)
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
        if (command.Request.SourceAccountId.HasValue)
            contribution.SourceAccountId = command.Request.SourceAccountId;
        if (command.Request.Category != null)
            contribution.Category = command.Request.Category;
        if (command.Request.AnnualIncreaseRate.HasValue)
            contribution.AnnualIncreaseRate = command.Request.AnnualIncreaseRate;
        if (command.Request.Notes != null)
            contribution.Notes = command.Request.Notes;
        if (command.Request.StartDate.HasValue)
            contribution.StartDate = command.Request.StartDate;
        if (command.Request.IsActive.HasValue)
            contribution.IsActive = command.Request.IsActive.Value;
        if (command.Request.EndConditionType.HasValue)
            contribution.EndConditionType = command.Request.EndConditionType.Value;
        if (command.Request.EndConditionAccountId.HasValue)
            contribution.EndConditionAccountId = command.Request.EndConditionAccountId;
        if (command.Request.EndDate.HasValue)
            contribution.EndDate = command.Request.EndDate;
        if (command.Request.EndAmountThreshold.HasValue)
            contribution.EndAmountThreshold = command.Request.EndAmountThreshold;

        contribution.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        // Re-query to get updated navigation properties
        var updated = await _db.InvestmentContributions
            .Include(c => c.TargetAccount)
            .Include(c => c.SourceAccount)
            .Include(c => c.EndConditionAccount)
            .FirstAsync(c => c.Id == contribution.Id, cancellationToken);

        return new InvestmentContributionDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Currency = updated.Currency,
            Amount = updated.Amount,
            Frequency = updated.Frequency,
            TargetAccountId = updated.TargetAccountId,
            TargetAccountName = updated.TargetAccount?.Name,
            SourceAccountId = updated.SourceAccountId,
            SourceAccountName = updated.SourceAccount?.Name,
            Category = updated.Category,
            AnnualIncreaseRate = updated.AnnualIncreaseRate,
            Notes = updated.Notes,
            StartDate = updated.StartDate,
            IsActive = updated.IsActive,
            EndConditionType = updated.EndConditionType,
            EndConditionAccountId = updated.EndConditionAccountId,
            EndConditionAccountName = updated.EndConditionAccount?.Name,
            EndDate = updated.EndDate,
            EndAmountThreshold = updated.EndAmountThreshold,
            CreatedAt = updated.CreatedAt
        };
    }
}
