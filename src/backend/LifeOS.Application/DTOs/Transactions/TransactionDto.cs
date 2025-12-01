using LifeOS.Application.DTOs.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Application.DTOs.Transactions;

public record TransactionDto
{
    public Guid Id { get; init; }
    public Guid? SourceAccountId { get; init; }
    public string? SourceAccountName { get; init; }
    public Guid? TargetAccountId { get; init; }
    public string? TargetAccountName { get; init; }
    public string Currency { get; init; } = "ZAR";
    public decimal Amount { get; init; }
    public decimal? AmountHomeCurrency { get; init; }
    public decimal? FxRateUsed { get; init; }
    public TransactionCategory Category { get; init; }
    public string? Subcategory { get; init; }
    public string[]? Tags { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public DateOnly TransactionDate { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = "manual";
    public bool IsReconciled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TransactionListResponse
{
    public List<TransactionItemResponse> Data { get; init; } = new();
    public PaginationMeta? Meta { get; init; }
    public PaginationLinks? Links { get; init; }
}

public record TransactionItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "transaction";
    public TransactionAttributes Attributes { get; init; } = new();
}

public record TransactionAttributes
{
    public Guid? SourceAccountId { get; init; }
    public string? SourceAccountName { get; init; }
    public Guid? TargetAccountId { get; init; }
    public string? TargetAccountName { get; init; }
    public string Currency { get; init; } = "ZAR";
    public decimal Amount { get; init; }
    public decimal? AmountHomeCurrency { get; init; }
    public decimal? FxRateUsed { get; init; }
    public string Category { get; init; } = string.Empty;
    public string? Subcategory { get; init; }
    public string[]? Tags { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public DateOnly TransactionDate { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = "manual";
    public bool IsReconciled { get; init; }
}

public record TransactionDetailResponse
{
    public TransactionItemResponse Data { get; init; } = new();
}

public record CreateTransactionRequest
{
    public Guid? SourceAccountId { get; init; }
    public Guid? TargetAccountId { get; init; }
    public string Currency { get; init; } = "ZAR";
    public decimal Amount { get; init; }
    public TransactionCategory Category { get; init; }
    public string? Subcategory { get; init; }
    public string[]? Tags { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public DateOnly TransactionDate { get; init; }
}

public record UpdateTransactionRequest
{
    public string? Subcategory { get; init; }
    public string[]? Tags { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public bool? IsReconciled { get; init; }
}
