using LifeOS.Domain.Enums;

namespace LifeOS.Application.DTOs.Finances;

public record InvestmentContributionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal Amount { get; init; }
    public PaymentFrequency Frequency { get; init; }
    public Guid? TargetAccountId { get; init; }
    public string? TargetAccountName { get; init; }
    public Guid? SourceAccountId { get; init; }
    public string? SourceAccountName { get; init; }
    public string? Category { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateOnly? StartDate { get; init; }
    public EndConditionType EndConditionType { get; init; }
    public Guid? EndConditionAccountId { get; init; }
    public string? EndConditionAccountName { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record InvestmentContributionListResponse
{
    public List<InvestmentContributionDto> Sources { get; init; } = new();
    public InvestmentContributionSummary Summary { get; init; } = new();
}

public record InvestmentContributionSummary
{
    public decimal TotalMonthlyContributions { get; init; }
    public Dictionary<string, decimal> ByCategory { get; init; } = new();
}

public record CreateInvestmentContributionRequest
{
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal Amount { get; init; }
    public PaymentFrequency Frequency { get; init; } = PaymentFrequency.Monthly;
    public Guid TargetAccountId { get; init; } // Required: where the investment goes
    public Guid SourceAccountId { get; init; } // Required: where the money comes from
    public string? Category { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? Notes { get; init; }
    public DateOnly? StartDate { get; init; } // Required for once-off contributions
    public EndConditionType EndConditionType { get; init; } = EndConditionType.None;
    public Guid? EndConditionAccountId { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
}

public record UpdateInvestmentContributionRequest
{
    public string? Name { get; init; }
    public decimal? Amount { get; init; }
    public PaymentFrequency? Frequency { get; init; }
    public Guid? TargetAccountId { get; init; }
    public Guid? SourceAccountId { get; init; }
    public string? Category { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? Notes { get; init; }
    public DateOnly? StartDate { get; init; }
    public bool? IsActive { get; init; }
    public EndConditionType? EndConditionType { get; init; }
    public Guid? EndConditionAccountId { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
}
