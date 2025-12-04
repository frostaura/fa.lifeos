using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Finances;

public class GetInvestmentContributionsQueryHandler 
    : IRequestHandler<GetInvestmentContributionsQuery, InvestmentContributionListResponse>
{
    private readonly ILifeOSDbContext _db;

    public GetInvestmentContributionsQueryHandler(ILifeOSDbContext db)
    {
        _db = db;
    }

    public async Task<InvestmentContributionListResponse> Handle(
        GetInvestmentContributionsQuery request,
        CancellationToken cancellationToken)
    {
        var contributions = await _db.InvestmentContributions
            .Include(c => c.TargetAccount)
            .Include(c => c.SourceAccount)
            .Include(c => c.EndConditionAccount)
            .Where(c => c.UserId == request.UserId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var dtos = contributions.Select(c => new InvestmentContributionDto
        {
            Id = c.Id,
            Name = c.Name,
            Currency = c.Currency,
            Amount = c.Amount,
            Frequency = c.Frequency,
            TargetAccountId = c.TargetAccountId,
            TargetAccountName = c.TargetAccount?.Name,
            SourceAccountId = c.SourceAccountId,
            SourceAccountName = c.SourceAccount?.Name,
            Category = c.Category,
            AnnualIncreaseRate = c.AnnualIncreaseRate,
            Notes = c.Notes,
            StartDate = c.StartDate,
            IsActive = c.IsActive,
            EndConditionType = c.EndConditionType,
            EndConditionAccountId = c.EndConditionAccountId,
            EndConditionAccountName = c.EndConditionAccount?.Name,
            EndDate = c.EndDate,
            EndAmountThreshold = c.EndAmountThreshold,
            CreatedAt = c.CreatedAt
        }).ToList();

        // Calculate monthly equivalents
        decimal GetMonthlyAmount(decimal amount, PaymentFrequency frequency) => frequency switch
        {
            PaymentFrequency.Weekly => amount * 4.33m,
            PaymentFrequency.Biweekly => amount * 2.17m,
            PaymentFrequency.Annually => amount / 12m,
            PaymentFrequency.Quarterly => amount / 3m,
            PaymentFrequency.Once => 0m, // One-time contributions don't count towards monthly total
            _ => amount
        };

        var totalMonthly = contributions
            .Where(c => c.IsActive && c.Frequency != PaymentFrequency.Once)
            .Sum(c => GetMonthlyAmount(c.Amount, c.Frequency));

        var byCategory = contributions
            .Where(c => c.IsActive && !string.IsNullOrEmpty(c.Category))
            .GroupBy(c => c.Category!)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(c => GetMonthlyAmount(c.Amount, c.Frequency))
            );

        return new InvestmentContributionListResponse
        {
            Sources = dtos,
            Summary = new InvestmentContributionSummary
            {
                TotalMonthlyContributions = totalMonthly,
                ByCategory = byCategory
            }
        };
    }
}
