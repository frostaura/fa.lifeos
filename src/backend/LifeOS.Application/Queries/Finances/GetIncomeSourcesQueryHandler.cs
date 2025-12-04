using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Finances;

public class GetIncomeSourcesQueryHandler : IRequestHandler<GetIncomeSourcesQuery, IncomeSourceListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetIncomeSourcesQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<IncomeSourceListResponse> Handle(GetIncomeSourcesQuery request, CancellationToken cancellationToken)
    {
        var incomeSources = await _context.IncomeSources
            .AsNoTracking()
            .Include(i => i.TaxProfile)
            .Include(i => i.TargetAccount)
            .Where(i => i.UserId == request.UserId)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        var totalMonthlyGross = incomeSources
            .Where(i => i.IsActive)
            .Sum(i => ConvertToMonthly(i.BaseAmount, i.PaymentFrequency));

        // Calculate net with actual tax deductions from tax profiles
        decimal totalMonthlyTax = 0m;
        decimal totalMonthlyUif = 0m;
        
        // Group income sources by tax profile to calculate tax correctly
        var incomeByTaxProfile = incomeSources
            .Where(i => i.IsActive && i.IsPreTax && i.TaxProfile != null)
            .GroupBy(i => i.TaxProfileId);

        foreach (var group in incomeByTaxProfile)
        {
            var taxProfile = group.First().TaxProfile!;
            
            // Calculate total annual income for this tax profile
            var totalAnnualIncome = group.Sum(i => ConvertToMonthly(i.BaseAmount, i.PaymentFrequency) * 12);
            
            // Calculate PAYE from tax brackets on aggregated income
            var annualTax = CalculateTaxFromBrackets(totalAnnualIncome, taxProfile.Brackets);
            
            // Apply age-based rebates from TaxRebates JSON (once per tax profile)
            var rebate = GetPrimaryRebate(taxProfile.TaxRebates);
            annualTax = Math.Max(0, annualTax - rebate);
            
            totalMonthlyTax += annualTax / 12;
            
            // Calculate UIF on total income (with single cap)
            if (taxProfile.UifRate.HasValue)
            {
                var totalMonthlyIncome = group.Sum(i => ConvertToMonthly(i.BaseAmount, i.PaymentFrequency));
                var monthlyUif = totalMonthlyIncome * taxProfile.UifRate.Value;
                if (taxProfile.UifCap.HasValue)
                    monthlyUif = Math.Min(monthlyUif, taxProfile.UifCap.Value);
                totalMonthlyUif += monthlyUif;
            }
        }

        var totalMonthlyNet = totalMonthlyGross - totalMonthlyTax - totalMonthlyUif;

        return new IncomeSourceListResponse
        {
            Data = incomeSources.Select(i => new IncomeSourceItemResponse
            {
                Id = i.Id,
                Type = "incomeSource",
                Attributes = new IncomeSourceAttributes
                {
                    Name = i.Name,
                    Currency = i.Currency,
                    BaseAmount = i.BaseAmount,
                    IsPreTax = i.IsPreTax,
                    TaxProfileId = i.TaxProfileId,
                    PaymentFrequency = i.PaymentFrequency.ToString().ToLowerInvariant(),
                    NextPaymentDate = i.NextPaymentDate,
                    AnnualIncreaseRate = i.AnnualIncreaseRate,
                    EmployerName = i.EmployerName,
                    Notes = i.Notes,
                    IsActive = i.IsActive,
                    TargetAccountId = i.TargetAccountId,
                    TargetAccountName = i.TargetAccount?.Name
                }
            }).ToList(),
            Meta = new IncomeSourceMeta
            {
                TotalMonthlyGross = totalMonthlyGross,
                TotalMonthlyNet = totalMonthlyNet,
                TotalMonthlyTax = totalMonthlyTax,
                TotalMonthlyUif = totalMonthlyUif
            }
        };
    }

    private static decimal ConvertToMonthly(decimal amount, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Weekly => amount * 52m / 12m,
            PaymentFrequency.Biweekly => amount * 26m / 12m,
            PaymentFrequency.Monthly => amount,
            PaymentFrequency.Quarterly => amount / 3m,
            PaymentFrequency.Annually => amount / 12m,
            PaymentFrequency.Once => 0m, // One-time items excluded from monthly totals
            _ => amount
        };
    }
    
    private static decimal CalculateTaxFromBrackets(decimal annualIncome, string bracketsJson)
    {
        try
        {
            var brackets = JsonSerializer.Deserialize<List<TaxBracket>>(bracketsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (brackets == null || brackets.Count == 0)
                return 0;

            decimal totalTax = 0;
            decimal remainingIncome = annualIncome;

            foreach (var bracket in brackets.OrderBy(b => b.Min))
            {
                if (remainingIncome <= 0 || annualIncome < bracket.Min)
                    break;

                var taxableInBracket = bracket.Max.HasValue
                    ? Math.Min(annualIncome, bracket.Max.Value) - bracket.Min
                    : annualIncome - bracket.Min;

                if (taxableInBracket > 0)
                {
                    totalTax = bracket.BaseTax + (taxableInBracket * bracket.Rate);
                }
            }

            return totalTax;
        }
        catch
        {
            return 0;
        }
    }
    
    private class TaxBracket
    {
        public decimal Min { get; set; }
        public decimal? Max { get; set; }
        public decimal Rate { get; set; }
        public decimal BaseTax { get; set; }
    }
    
    private static decimal GetPrimaryRebate(string? taxRebatesJson)
    {
        if (string.IsNullOrEmpty(taxRebatesJson))
            return 0;
            
        try
        {
            var rebates = JsonSerializer.Deserialize<TaxRebates>(taxRebatesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return rebates?.Primary ?? 0;
        }
        catch
        {
            return 0;
        }
    }
    
    private class TaxRebates
    {
        public decimal Primary { get; set; }
        public decimal Secondary { get; set; }
        public decimal Tertiary { get; set; }
    }
}
