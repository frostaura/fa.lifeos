using LifeOS.Application.DTOs.Common;

namespace LifeOS.Application.DTOs.Milestones;

public record MilestoneDto
{
    public Guid Id { get; init; }
    public Guid DimensionId { get; init; }
    public string? DimensionCode { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateOnly? TargetDate { get; init; }
    public string? TargetMetricCode { get; init; }
    public decimal? TargetMetricValue { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record MilestoneListResponse
{
    public List<MilestoneItemResponse> Data { get; init; } = new();
    public PaginationMeta? Meta { get; init; }
}

public record MilestoneItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "milestone";
    public MilestoneAttributes Attributes { get; init; } = new();
}

public record MilestoneAttributes
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid DimensionId { get; init; }
    public string? DimensionCode { get; init; }
    public DateOnly? TargetDate { get; init; }
    public string? TargetMetricCode { get; init; }
    public decimal? TargetMetricValue { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record MilestoneDetailResponse
{
    public MilestoneItemResponse Data { get; init; } = new();
}

public record CreateMilestoneRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid DimensionId { get; init; }
    public DateOnly? TargetDate { get; init; }
    public string? TargetMetricCode { get; init; }
    public decimal? TargetMetricValue { get; init; }
}

public record UpdateMilestoneRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateOnly? TargetDate { get; init; }
    public string? Status { get; init; }
}
