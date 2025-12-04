using LifeOS.Application.DTOs.Milestones;
using LifeOS.Domain.Enums;

namespace LifeOS.Application.DTOs.Finances;

public record IncomeSourceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal BaseAmount { get; init; }
    public bool IsPreTax { get; init; }
    public Guid? TaxProfileId { get; init; }
    public PaymentFrequency PaymentFrequency { get; init; }
    public DateOnly? NextPaymentDate { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? EmployerName { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public Guid? TargetAccountId { get; init; }
    public string? TargetAccountName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record IncomeSourceListResponse
{
    public List<IncomeSourceItemResponse> Data { get; init; } = new();
    public IncomeSourceMeta? Meta { get; init; }
}

public record IncomeSourceItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "incomeSource";
    public IncomeSourceAttributes Attributes { get; init; } = new();
}

public record IncomeSourceAttributes
{
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal BaseAmount { get; init; }
    public bool IsPreTax { get; init; }
    public Guid? TaxProfileId { get; init; }
    public string PaymentFrequency { get; init; } = string.Empty;
    public DateOnly? NextPaymentDate { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? EmployerName { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public Guid? TargetAccountId { get; init; }
    public string? TargetAccountName { get; init; }
}

public record IncomeSourceMeta
{
    public decimal TotalMonthlyGross { get; init; }
    public decimal TotalMonthlyNet { get; init; }
    public decimal TotalMonthlyTax { get; init; }
    public decimal TotalMonthlyUif { get; init; }
}

public record IncomeSourceDetailResponse
{
    public IncomeSourceItemResponse Data { get; init; } = new();
}

public record CreateIncomeSourceRequest
{
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal BaseAmount { get; init; }
    public bool IsPreTax { get; init; } = true;
    public Guid? TaxProfileId { get; init; }
    public PaymentFrequency PaymentFrequency { get; init; } = PaymentFrequency.Monthly;
    public DateOnly? NextPaymentDate { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? EmployerName { get; init; }
    public string? Notes { get; init; }
    public Guid? TargetAccountId { get; init; } // Optional: where the income is deposited
}

public record UpdateIncomeSourceRequest
{
    public string? Name { get; init; }
    public decimal? BaseAmount { get; init; }
    public Guid? TaxProfileId { get; init; }
    public bool ClearTaxProfile { get; init; } = false;
    public PaymentFrequency? PaymentFrequency { get; init; }
    public DateOnly? NextPaymentDate { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? EmployerName { get; init; }
    public string? Notes { get; init; }
    public bool? IsActive { get; init; }
    public Guid? TargetAccountId { get; init; }
}
