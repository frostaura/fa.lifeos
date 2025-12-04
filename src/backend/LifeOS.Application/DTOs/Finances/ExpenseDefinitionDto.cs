using LifeOS.Application.DTOs.Milestones;
using LifeOS.Domain.Enums;

namespace LifeOS.Application.DTOs.Finances;

public record ExpenseDefinitionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public AmountType AmountType { get; init; }
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public PaymentFrequency Frequency { get; init; }
    public DateOnly? StartDate { get; init; }
    public string Category { get; init; } = string.Empty;
    public bool IsTaxDeductible { get; init; }
    public Guid? LinkedAccountId { get; init; }
    public bool InflationAdjusted { get; init; }
    public bool IsActive { get; init; }
    public EndConditionType EndConditionType { get; init; }
    public Guid? EndConditionAccountId { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ExpenseDefinitionListResponse
{
    public List<ExpenseDefinitionItemResponse> Data { get; init; } = new();
    public ExpenseDefinitionMeta? Meta { get; init; }
}

public record ExpenseDefinitionItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "expenseDefinition";
    public ExpenseDefinitionAttributes Attributes { get; init; } = new();
}

public record ExpenseDefinitionAttributes
{
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public string AmountType { get; init; } = string.Empty;
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public string? StartDate { get; init; }
    public string Category { get; init; } = string.Empty;
    public bool IsTaxDeductible { get; init; }
    public Guid? LinkedAccountId { get; init; }
    public string? LinkedAccountName { get; init; }
    public bool InflationAdjusted { get; init; }
    public bool IsActive { get; init; }
    public string EndConditionType { get; init; } = "none";
    public Guid? EndConditionAccountId { get; init; }
    public string? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
}

public record ExpenseDefinitionMeta
{
    public decimal TotalMonthly { get; init; }
    public Dictionary<string, decimal> ByCategory { get; init; } = new();
}

public record ExpenseDefinitionDetailResponse
{
    public ExpenseDefinitionItemResponse Data { get; init; } = new();
}

public record CreateExpenseDefinitionRequest
{
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public AmountType AmountType { get; init; } = AmountType.Fixed;
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public PaymentFrequency Frequency { get; init; } = PaymentFrequency.Monthly;
    public DateOnly? StartDate { get; init; } // Start date for scheduling (especially for once-off expenses)
    public string Category { get; init; } = string.Empty;
    public bool IsTaxDeductible { get; init; }
    public Guid? LinkedAccountId { get; init; } // Optional: the account this expense is debited from
    public bool InflationAdjusted { get; init; } = true;
    public EndConditionType EndConditionType { get; init; } = EndConditionType.None;
    public Guid? EndConditionAccountId { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
}

public record UpdateExpenseDefinitionRequest
{
    public string? Name { get; init; }
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public PaymentFrequency? Frequency { get; init; }
    public DateOnly? StartDate { get; init; }
    public string? Category { get; init; }
    public bool? IsTaxDeductible { get; init; }
    public Guid? LinkedAccountId { get; init; }
    public bool? InflationAdjusted { get; init; }
    public bool? IsActive { get; init; }
    public EndConditionType? EndConditionType { get; init; }
    public Guid? EndConditionAccountId { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
}
