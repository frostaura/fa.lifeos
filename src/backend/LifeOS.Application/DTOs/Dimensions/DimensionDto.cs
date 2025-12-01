namespace LifeOS.Application.DTOs.Dimensions;

public record DimensionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public decimal Weight { get; init; }
    public decimal DefaultWeight { get; init; }
    public short SortOrder { get; init; }
    public bool IsActive { get; init; }
    public decimal CurrentScore { get; init; }
}

public record DimensionListResponse
{
    public List<DimensionItemResponse> Data { get; init; } = new();
    public DimensionListMeta Meta { get; init; } = new();
}

public record DimensionItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "dimension";
    public DimensionAttributes Attributes { get; init; } = new();
}

public record DimensionAttributes
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public decimal Weight { get; init; }
    public decimal DefaultWeight { get; init; }
    public short SortOrder { get; init; }
    public bool IsActive { get; init; }
    public decimal CurrentScore { get; init; }
}

public record DimensionListMeta
{
    public decimal TotalWeight { get; init; }
}

public record DimensionDetailResponse
{
    public DimensionDetailData Data { get; init; } = new();
}

public record DimensionDetailData
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "dimension";
    public DimensionAttributes Attributes { get; init; } = new();
    public DimensionRelationships? Relationships { get; init; }
}

public record DimensionRelationships
{
    public List<MilestoneReference> Milestones { get; init; } = new();
    public List<TaskReference> ActiveTasks { get; init; } = new();
}

public record MilestoneReference
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public record TaskReference
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string TaskType { get; init; } = string.Empty;
}
