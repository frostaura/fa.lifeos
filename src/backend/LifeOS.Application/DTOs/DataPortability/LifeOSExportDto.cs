namespace LifeOS.Application.DTOs.DataPortability;

public record LifeOSExportDto
{
    public ExportSchemaDto Schema { get; init; } = new();
    public ExportDataDto Data { get; init; } = new();
    public ExportMetaDto? Meta { get; init; }
}

public record ExportMetaDto
{
    public int TotalEntities { get; init; }
    public EntityCountsDto EntityCounts { get; init; } = new();
}

public record EntityCountsDto
{
    public int Dimensions { get; init; }
    public int MetricDefinitions { get; init; }
    public int ScoreDefinitions { get; init; }
    public int TaxProfiles { get; init; }
    public int LongevityModels { get; init; }
    public int Accounts { get; init; }
    public int Milestones { get; init; }
    public int Tasks { get; init; }
    public int Streaks { get; init; }
    public int MetricRecords { get; init; }
    public int ScoreRecords { get; init; }
    public int IncomeSources { get; init; }
    public int ExpenseDefinitions { get; init; }
    public int InvestmentContributions { get; init; }
    public int FinancialGoals { get; init; }
    public int FxRates { get; init; }
    public int Transactions { get; init; }
    public int SimulationScenarios { get; init; }
    public int SimulationEvents { get; init; }
    public int AccountProjections { get; init; }
    public int NetWorthProjections { get; init; }
    public int LongevitySnapshots { get; init; }
    public int Achievements { get; init; }
    public int UserAchievements { get; init; }
    public int UserXP { get; init; }
    public int NetWorthSnapshots { get; init; }
}

public record ImportRequestDto
{
    public string Mode { get; init; } = "replace";
    public bool DryRun { get; init; } = false;
    public LifeOSExportDto? Data { get; init; }
}

public record ImportResultDto
{
    public string Status { get; init; } = "completed";
    public string Mode { get; init; } = "replace";
    public DateTime ImportedAt { get; init; } = DateTime.UtcNow;
    public string SchemaVersion { get; init; } = "1.0.0";
    public Dictionary<string, ImportEntityResultDto> Results { get; init; } = new();
    public int TotalImported { get; init; }
    public int TotalSkipped { get; init; }
    public int TotalErrors { get; init; }
    public long DurationMs { get; init; }
    public bool IsDryRun { get; init; }
}

public record ImportEntityResultDto
{
    public int Imported { get; init; }
    public int Skipped { get; init; }
    public int Errors { get; init; }
    public List<string>? ErrorDetails { get; init; }
}
