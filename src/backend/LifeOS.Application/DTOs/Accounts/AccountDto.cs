using LifeOS.Application.DTOs.Common;
using LifeOS.Application.DTOs.Milestones;
using LifeOS.Domain.Enums;

namespace LifeOS.Application.DTOs.Accounts;

public record AccountDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    public string Currency { get; init; } = "ZAR";
    public decimal InitialBalance { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal? CurrentBalanceHomeCurrency { get; init; }
    public DateTime BalanceUpdatedAt { get; init; }
    public bool IsLiability { get; init; }
    public decimal? InterestRateAnnual { get; init; }
    public CompoundingFrequency? InterestCompounding { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AccountListResponse
{
    public List<AccountItemResponse> Data { get; init; } = new();
    public AccountListMeta? Meta { get; init; }
    public PaginationLinks? Links { get; init; }
}

public record AccountItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "account";
    public AccountAttributes Attributes { get; init; } = new();
}

public record AccountAttributes
{
    public string Name { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal InitialBalance { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal? CurrentBalanceHomeCurrency { get; init; }
    public DateTime BalanceUpdatedAt { get; init; }
    public string? Institution { get; init; }
    public bool IsLiability { get; init; }
    public decimal? InterestRateAnnual { get; init; }
    public string? InterestCompounding { get; init; }
    public decimal? MonthlyInterest { get; init; }
    public decimal MonthlyFee { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public bool IsActive { get; init; }
}

public record AccountListMeta
{
    public int Page { get; init; }
    public int PerPage { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public decimal NetWorth { get; init; }
    public decimal TotalMonthlyInterest { get; init; }
    public decimal TotalMonthlyFees { get; init; }
    public string HomeCurrency { get; init; } = "ZAR";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record AccountDetailResponse
{
    public AccountItemResponse Data { get; init; } = new();
    public ApiMeta? Meta { get; init; }
}

public record CreateAccountRequest
{
    public string Name { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    public string Currency { get; init; } = "ZAR";
    public decimal InitialBalance { get; init; }
    public string? Institution { get; init; }
    public bool IsLiability { get; init; }
    public decimal? InterestRateAnnual { get; init; }
    public CompoundingFrequency? InterestCompounding { get; init; }
    public decimal MonthlyFee { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record UpdateAccountRequest
{
    public string? Name { get; init; }
    public AccountType? AccountType { get; init; }
    public string? Currency { get; init; }
    public decimal? CurrentBalance { get; init; }
    public string? Institution { get; init; }
    public bool? IsLiability { get; init; }
    public decimal? InterestRateAnnual { get; init; }
    public CompoundingFrequency? InterestCompounding { get; init; }
    public decimal? MonthlyFee { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public bool? IsActive { get; init; }
}

public record AccountBalanceResponse
{
    public AccountBalanceData Data { get; init; } = new();
}

public record AccountBalanceData
{
    public Guid AccountId { get; init; }
    public string OriginalCurrency { get; init; } = string.Empty;
    public decimal OriginalBalance { get; init; }
    public string TargetCurrency { get; init; } = string.Empty;
    public decimal ConvertedBalance { get; init; }
    public decimal FxRate { get; init; }
    public DateTime FxRateTimestamp { get; init; }
    public string FxSource { get; init; } = "coingecko";
}
