using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public record CreateInvestmentContributionCommand(
    Guid UserId,
    CreateInvestmentContributionRequest Request
) : IRequest<InvestmentContributionDto>;

public class CreateInvestmentContributionCommandHandler 
    : IRequestHandler<CreateInvestmentContributionCommand, InvestmentContributionDto>
{
    private readonly ILifeOSDbContext _db;

    public CreateInvestmentContributionCommandHandler(ILifeOSDbContext db)
    {
        _db = db;
    }

    public async Task<InvestmentContributionDto> Handle(
        CreateInvestmentContributionCommand command,
        CancellationToken cancellationToken)
    {
        var contribution = new InvestmentContribution
        {
            UserId = command.UserId,
            Name = command.Request.Name,
            Currency = command.Request.Currency,
            Amount = command.Request.Amount,
            Frequency = command.Request.Frequency,
            TargetAccountId = command.Request.TargetAccountId,
            SourceAccountId = command.Request.SourceAccountId,
            Category = command.Request.Category,
            AnnualIncreaseRate = command.Request.AnnualIncreaseRate,
            Notes = command.Request.Notes,
            StartDate = command.Request.StartDate,
            IsActive = true,
            EndConditionType = command.Request.EndConditionType,
            EndConditionAccountId = command.Request.EndConditionAccountId,
            EndDate = command.Request.EndDate,
            EndAmountThreshold = command.Request.EndAmountThreshold,
        };

        _db.InvestmentContributions.Add(contribution);
        await _db.SaveChangesAsync(cancellationToken);

        return new InvestmentContributionDto
        {
            Id = contribution.Id,
            Name = contribution.Name,
            Currency = contribution.Currency,
            Amount = contribution.Amount,
            Frequency = contribution.Frequency,
            TargetAccountId = contribution.TargetAccountId,
            SourceAccountId = contribution.SourceAccountId,
            Category = contribution.Category,
            AnnualIncreaseRate = contribution.AnnualIncreaseRate,
            Notes = contribution.Notes,
            StartDate = contribution.StartDate,
            IsActive = contribution.IsActive,
            EndConditionType = contribution.EndConditionType,
            EndConditionAccountId = contribution.EndConditionAccountId,
            EndDate = contribution.EndDate,
            EndAmountThreshold = contribution.EndAmountThreshold,
            CreatedAt = contribution.CreatedAt
        };
    }
}
