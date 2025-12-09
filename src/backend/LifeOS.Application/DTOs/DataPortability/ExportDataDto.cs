

namespace LifeOS.Application.DTOs.DataPortability;

public record ExportDataDto
{
    public ProfileExportDto? Profile { get; init; }
    public List<DimensionExportDto> Dimensions { get; init; } = new();
    public List<MetricDefinitionExportDto> MetricDefinitions { get; init; } = new();
    public List<ScoreDefinitionExportDto> ScoreDefinitions { get; init; } = new();
    public List<TaxProfileExportDto> TaxProfiles { get; init; } = new();
    public List<LongevityModelExportDto> LongevityModels { get; init; } = new();
    public List<AccountExportDto> Accounts { get; init; } = new();
    public List<MilestoneExportDto> Milestones { get; init; } = new();
    public List<LifeTaskExportDto> Tasks { get; init; } = new();
    public List<StreakExportDto> Streaks { get; init; } = new();
    public List<MetricRecordExportDto> MetricRecords { get; init; } = new();
    public List<ScoreRecordExportDto> ScoreRecords { get; init; } = new();
    public List<IncomeSourceExportDto> IncomeSources { get; init; } = new();
    public List<ExpenseDefinitionExportDto> ExpenseDefinitions { get; init; } = new();
    public List<InvestmentContributionExportDto> InvestmentContributions { get; init; } = new();
    public List<FinancialGoalExportDto> FinancialGoals { get; init; } = new();
    public List<FxRateExportDto> FxRates { get; init; } = new();
    public List<TransactionExportDto> Transactions { get; init; } = new();
    public List<SimulationScenarioExportDto> SimulationScenarios { get; init; } = new();
    public List<SimulationEventExportDto> SimulationEvents { get; init; } = new();
    public List<AccountProjectionExportDto> AccountProjections { get; init; } = new();
    public List<NetWorthProjectionExportDto> NetWorthProjections { get; init; } = new();
    public List<LongevitySnapshotExportDto> LongevitySnapshots { get; init; } = new();
    public List<AchievementExportDto> Achievements { get; init; } = new();
    public List<UserAchievementExportDto> UserAchievements { get; init; } = new();
    public UserXPExportDto? UserXP { get; init; }
    public List<NetWorthSnapshotExportDto> NetWorthSnapshots { get; init; } = new();
}

public record ProfileExportDto
{
    public string? Email { get; init; }
    public string? Username { get; init; }
    public string HomeCurrency { get; init; } = "ZAR";
    public DateOnly? DateOfBirth { get; init; }
    public int LifeExpectancyBaseline { get; init; }
    public decimal? InflationRateAnnual { get; init; }
    public decimal? DefaultGrowthRate { get; init; }
    public int? RetirementAge { get; init; }
}

public record DimensionExportDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public decimal DefaultWeight { get; init; }
    public short SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public record MetricDefinitionExportDto
{
    public Guid Id { get; init; }
    public Guid? DimensionId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Unit { get; init; }
    public string ValueType { get; init; } = "number";
    public string AggregationType { get; init; } = "last";
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public decimal? TargetValue { get; init; }
    public string? Icon { get; init; }
    public string[]? Tags { get; init; }
    public string[]? EnumValues { get; init; }
    public bool IsDerived { get; init; }
    public string? DerivedFormula { get; init; }
    public bool IsActive { get; init; }
}

public record ScoreDefinitionExportDto
{
    public Guid Id { get; init; }
    public Guid? DimensionId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Formula { get; init; }
    public decimal MinScore { get; init; }
    public decimal MaxScore { get; init; }
    public bool IsActive { get; init; }
}

public record TaxProfileExportDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int TaxYear { get; init; }
    public string CountryCode { get; init; } = "ZA";
    public string? Brackets { get; init; }
    public decimal? UifRate { get; init; }
    public decimal? UifCap { get; init; }
    public decimal? VatRate { get; init; }
    public bool IsVatRegistered { get; init; }
    public string? TaxRebates { get; init; }
    public bool IsActive { get; init; }
}

public record LongevityModelExportDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string[]? InputMetrics { get; init; }
    public string ModelType { get; init; } = "linear";
    public string? Parameters { get; init; }
    public string OutputUnit { get; init; } = "years_added";
    public bool IsActive { get; init; }
}

public record AccountExportDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal InitialBalance { get; init; }
    public decimal CurrentBalance { get; init; }
    public DateTime BalanceUpdatedAt { get; init; }
    public string? Institution { get; init; }
    public bool IsLiability { get; init; }
    public decimal? InterestRateAnnual { get; init; }
    public string? InterestCompounding { get; init; }
    public decimal? MonthlyFee { get; init; }
    public string? Metadata { get; init; }
    public bool IsActive { get; init; }
}

public record MilestoneExportDto
{
    public Guid Id { get; init; }
    public Guid DimensionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateOnly? TargetDate { get; init; }
    public string? TargetMetricCode { get; init; }
    public decimal? TargetMetricValue { get; init; }
    public string Status { get; init; } = "active";
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record LifeTaskExportDto
{
    public Guid Id { get; init; }
    public Guid? DimensionId { get; init; }
    public Guid? MilestoneId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TaskType { get; init; } = "habit";
    public string? Frequency { get; init; }
    public string? LinkedMetricCode { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public TimeOnly? ScheduledTime { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsActive { get; init; }
    public string[]? Tags { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record StreakExportDto
{
    public Guid Id { get; init; }
    public Guid? TaskId { get; init; }
    public string? MetricCode { get; init; }
    public int CurrentStreakLength { get; init; }
    public int LongestStreakLength { get; init; }
    public DateOnly? LastSuccessDate { get; init; }
    public DateOnly? StreakStartDate { get; init; }
    public int MissCount { get; init; }
    public int MaxAllowedMisses { get; init; }
    public bool IsActive { get; init; }
}

public record MetricRecordExportDto
{
    public Guid Id { get; init; }
    public string MetricCode { get; init; } = string.Empty;
    public decimal? ValueNumber { get; init; }
    public bool? ValueBoolean { get; init; }
    public string? ValueString { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = "manual";
    public string? Notes { get; init; }
    public string? Metadata { get; init; }
}

public record ScoreRecordExportDto
{
    public Guid Id { get; init; }
    public string ScoreCode { get; init; } = string.Empty;
    public decimal ScoreValue { get; init; }
    public string PeriodType { get; init; } = "daily";
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public string? Breakdown { get; init; }
    public DateTime CalculatedAt { get; init; }
}

public record IncomeSourceExportDto
{
    public Guid Id { get; init; }
    public Guid? TaxProfileId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal BaseAmount { get; init; }
    public bool IsPreTax { get; init; }
    public string PaymentFrequency { get; init; } = "Monthly";
    public DateOnly? NextPaymentDate { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? EmployerName { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public Guid? TargetAccountId { get; init; }
}

public record ExpenseDefinitionExportDto
{
    public Guid Id { get; init; }
    public Guid? LinkedAccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public string AmountType { get; init; } = "Fixed";
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public string Frequency { get; init; } = "Monthly";
    public DateOnly? StartDate { get; init; }
    public string Category { get; init; } = "Other";
    public bool IsTaxDeductible { get; init; }
    public bool InflationAdjusted { get; init; }
    public bool IsActive { get; init; }
    public string EndConditionType { get; init; } = "None";
    public Guid? EndConditionAccountId { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
}

public record InvestmentContributionExportDto
{
    public Guid Id { get; init; }
    public Guid? TargetAccountId { get; init; }
    public Guid? SourceAccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = "ZAR";
    public decimal Amount { get; init; }
    public string Frequency { get; init; } = "Monthly";
    public string? Category { get; init; }
    public decimal? AnnualIncreaseRate { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateOnly? StartDate { get; init; }
    public string EndConditionType { get; init; } = "None";
    public Guid? EndConditionAccountId { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? EndAmountThreshold { get; init; }
}

public record FinancialGoalExportDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public decimal CurrentAmount { get; init; }
    public string Currency { get; init; } = "ZAR";
    public DateTime? TargetDate { get; init; }
    public int Priority { get; init; }
    public string? Category { get; init; }
    public string? IconName { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}

public record FxRateExportDto
{
    public Guid Id { get; init; }
    public string BaseCurrency { get; init; } = string.Empty;
    public string QuoteCurrency { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateTime RateTimestamp { get; init; }
    public string Source { get; init; } = "coingecko";
}

public record TransactionExportDto
{
    public Guid Id { get; init; }
    public Guid? SourceAccountId { get; init; }
    public Guid? TargetAccountId { get; init; }
    public string Currency { get; init; } = "ZAR";
    public decimal Amount { get; init; }
    public decimal? AmountHomeCurrency { get; init; }
    public decimal? FxRateUsed { get; init; }
    public string Category { get; init; } = "expense";
    public string? Subcategory { get; init; }
    public string[]? Tags { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public DateOnly TransactionDate { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = "manual";
    public bool IsReconciled { get; init; }
}

public record SimulationScenarioExportDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? EndCondition { get; init; }
    public string BaseAssumptions { get; init; } = "{}";
    public bool IsBaseline { get; init; }
    public DateTime? LastRunAt { get; init; }
}

public record SimulationEventExportDto
{
    public Guid Id { get; init; }
    public Guid ScenarioId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TriggerType { get; init; } = "Date";
    public DateOnly? TriggerDate { get; init; }
    public short? TriggerAge { get; init; }
    public string? TriggerCondition { get; init; }
    public string EventType { get; init; } = "expense";
    public string? Currency { get; init; }
    public string AmountType { get; init; } = "Fixed";
    public decimal? AmountValue { get; init; }
    public Guid? AffectedAccountId { get; init; }
    public bool AppliesOnce { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public record AccountProjectionExportDto
{
    public Guid Id { get; init; }
    public Guid ScenarioId { get; init; }
    public Guid AccountId { get; init; }
    public DateOnly PeriodDate { get; init; }
    public decimal Balance { get; init; }
    public decimal? BalanceHomeCurrency { get; init; }
    public decimal? PeriodIncome { get; init; }
    public decimal? PeriodExpenses { get; init; }
    public decimal? PeriodInterest { get; init; }
}

public record NetWorthProjectionExportDto
{
    public Guid Id { get; init; }
    public Guid ScenarioId { get; init; }
    public DateOnly PeriodDate { get; init; }
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public decimal NetWorth { get; init; }
    public string? BreakdownByType { get; init; }
    public string? BreakdownByCurrency { get; init; }
}

public record LongevitySnapshotExportDto
{
    public Guid Id { get; init; }
    public DateTime CalculatedAt { get; init; }
    public decimal BaselineLifeExpectancy { get; init; }
    public decimal EstimatedYearsAdded { get; init; }
    public decimal AdjustedLifeExpectancy { get; init; }
    public string? Breakdown { get; init; }
    public string? InputMetricsSnapshot { get; init; }
    public string? ConfidenceLevel { get; init; }
}

public record AchievementExportDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public int XpValue { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Tier { get; init; } = "bronze";
    public string UnlockCondition { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public record UserAchievementExportDto
{
    public Guid Id { get; init; }
    public string AchievementCode { get; init; } = string.Empty;
    public DateTime UnlockedAt { get; init; }
    public int Progress { get; init; }
    public string? UnlockContext { get; init; }
}

public record UserXPExportDto
{
    public Guid Id { get; init; }
    public long TotalXp { get; init; }
    public int Level { get; init; }
    public int WeeklyXp { get; init; }
    public DateOnly WeekStartDate { get; init; }
}

public record NetWorthSnapshotExportDto
{
    public Guid Id { get; init; }
    public DateOnly SnapshotDate { get; init; }
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public decimal NetWorth { get; init; }
    public string HomeCurrency { get; init; } = "ZAR";
    public string BreakdownByType { get; init; } = "{}";
    public string BreakdownByCurrency { get; init; } = "{}";
    public int AccountCount { get; init; }
}
