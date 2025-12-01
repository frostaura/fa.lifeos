namespace LifeOS.Application.DTOs.Scores;

// Scores Response
public record ScoresResponse
{
    public List<ScoreItemResponse> Data { get; init; } = new();
}

public record ScoreItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "score";
    public ScoreAttributes Attributes { get; init; } = new();
}

public record ScoreAttributes
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? DimensionId { get; init; }
    public string? DimensionCode { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal? PreviousValue { get; init; }
    public decimal? Change { get; init; }
    public decimal? ChangePercent { get; init; }
    public string PeriodType { get; init; } = "daily";
    public decimal MinScore { get; init; }
    public decimal MaxScore { get; init; }
}

// Streaks Response
public record StreaksResponse
{
    public List<StreakItemResponse> Data { get; init; } = new();
}

public record StreakItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "streak";
    public StreakAttributes Attributes { get; init; } = new();
}

public record StreakAttributes
{
    public Guid? TaskId { get; init; }
    public string? TaskTitle { get; init; }
    public string? MetricCode { get; init; }
    public int CurrentStreakLength { get; init; }
    public int LongestStreakLength { get; init; }
    public DateOnly? LastSuccessDate { get; init; }
    public DateOnly? StreakStartDate { get; init; }
    public int MissCount { get; init; }
    public int MaxAllowedMisses { get; init; }
    public bool IsActive { get; init; }
}
