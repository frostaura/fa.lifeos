namespace LifeOS.Application.DTOs.Common;

public record PaginationLinks
{
    public string? First { get; init; }
    public string? Prev { get; init; }
    public string? Next { get; init; }
    public string? Last { get; init; }
}

public record PaginationMeta
{
    public int Page { get; init; }
    public int PerPage { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record ApiMeta
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
