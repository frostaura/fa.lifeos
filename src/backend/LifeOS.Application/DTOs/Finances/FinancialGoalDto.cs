namespace LifeOS.Application.DTOs.Finances;

public record FinancialGoalDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public decimal CurrentAmount { get; init; }
    public int Priority { get; init; }
    public DateTime? TargetDate { get; init; }
    public string? Category { get; init; }
    public string? IconName { get; init; }
    public string Currency { get; init; } = "ZAR";
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    
    // Computed fields
    public decimal RemainingAmount { get; init; }
    public decimal ProgressPercent { get; init; }
    public int? MonthsToAcquire { get; init; } // Based on monthly savings rate
}

public record FinancialGoalListResponse
{
    public List<FinancialGoalDto> Goals { get; init; } = new();
    public FinancialGoalSummary Summary { get; init; } = new();
}

public record FinancialGoalSummary
{
    public decimal TotalTargetAmount { get; init; }
    public decimal TotalCurrentAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    public decimal OverallProgressPercent { get; init; }
    public decimal MonthlyInvestmentRate { get; init; } // Total monthly investment contributions
}

public record CreateFinancialGoalRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public decimal CurrentAmount { get; init; } = 0;
    public int Priority { get; init; } = 1;
    public DateTime? TargetDate { get; init; }
    public string? Category { get; init; }
    public string? IconName { get; init; }
    public string Currency { get; init; } = "ZAR";
    public string? Notes { get; init; }
}

public record UpdateFinancialGoalRequest
{
    public string? Name { get; init; }
    public decimal? TargetAmount { get; init; }
    public decimal? CurrentAmount { get; init; }
    public int? Priority { get; init; }
    public DateTime? TargetDate { get; init; }
    public string? Category { get; init; }
    public string? IconName { get; init; }
    public string? Notes { get; init; }
    public bool? IsActive { get; init; }
}
