using LifeOS.Domain.Enums;

namespace LifeOS.Application.DTOs.Simulations;

// === Scenario DTOs ===

public record SimulationScenarioDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? EndCondition { get; init; }
    public Dictionary<string, object>? BaseAssumptions { get; init; }
    public bool IsBaseline { get; init; }
    public DateTime? LastRunAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ScenarioListResponse
{
    public List<ScenarioItemResponse> Data { get; init; } = new();
    public ScenarioListMeta? Meta { get; init; }
}

public record ScenarioItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "simulationScenario";
    public ScenarioAttributes Attributes { get; init; } = new();
}

public record ScenarioAttributes
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? EndCondition { get; init; }
    public Dictionary<string, object>? BaseAssumptions { get; init; }
    public bool IsBaseline { get; init; }
    public DateTime? LastRunAt { get; init; }
}

public record ScenarioListMeta
{
    public int Total { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record ScenarioDetailResponse
{
    public ScenarioItemResponse Data { get; init; } = new();
    public SimulationMeta? Meta { get; init; }
}

public record SimulationMeta
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// === Scenario Requests ===

public record CreateScenarioRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? EndCondition { get; init; }
    public Dictionary<string, object>? BaseAssumptions { get; init; }
    public bool IsBaseline { get; init; }
}

public record UpdateScenarioRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? EndCondition { get; init; }
    public Dictionary<string, object>? BaseAssumptions { get; init; }
    public bool? IsBaseline { get; init; }
}

public record RunSimulationRequest
{
    public bool RecalculateFromStart { get; init; } = true;
}

// === Event DTOs ===

public record SimulationEventDto
{
    public Guid Id { get; init; }
    public Guid ScenarioId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public SimTriggerType TriggerType { get; init; }
    public DateOnly? TriggerDate { get; init; }
    public short? TriggerAge { get; init; }
    public string? TriggerCondition { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? Currency { get; init; }
    public AmountType AmountType { get; init; }
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public Guid? AffectedAccountId { get; init; }
    public bool AppliesOnce { get; init; }
    public PaymentFrequency? RecurrenceFrequency { get; init; }
    public DateOnly? RecurrenceEndDate { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public record EventListResponse
{
    public List<EventItemResponse> Data { get; init; } = new();
    public SimulationMeta? Meta { get; init; }
}

public record EventItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "simulationEvent";
    public EventAttributes Attributes { get; init; } = new();
}

public record EventAttributes
{
    public Guid ScenarioId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TriggerType { get; init; } = string.Empty;
    public DateOnly? TriggerDate { get; init; }
    public short? TriggerAge { get; init; }
    public string? TriggerCondition { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? Currency { get; init; }
    public string AmountType { get; init; } = string.Empty;
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public Guid? AffectedAccountId { get; init; }
    public bool AppliesOnce { get; init; }
    public string? RecurrenceFrequency { get; init; }
    public DateOnly? RecurrenceEndDate { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public record EventDetailResponse
{
    public EventItemResponse Data { get; init; } = new();
    public SimulationMeta? Meta { get; init; }
}

// === Event Requests ===

public record CreateEventRequest
{
    public Guid ScenarioId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public SimTriggerType TriggerType { get; init; }
    public DateOnly? TriggerDate { get; init; }
    public short? TriggerAge { get; init; }
    public string? TriggerCondition { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? Currency { get; init; }
    public AmountType AmountType { get; init; }
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public Guid? AffectedAccountId { get; init; }
    public bool AppliesOnce { get; init; } = true;
    public PaymentFrequency? RecurrenceFrequency { get; init; }
    public DateOnly? RecurrenceEndDate { get; init; }
    public int SortOrder { get; init; }
}

public record UpdateEventRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public SimTriggerType? TriggerType { get; init; }
    public DateOnly? TriggerDate { get; init; }
    public short? TriggerAge { get; init; }
    public string? TriggerCondition { get; init; }
    public string? EventType { get; init; }
    public string? Currency { get; init; }
    public AmountType? AmountType { get; init; }
    public decimal? AmountValue { get; init; }
    public string? AmountFormula { get; init; }
    public Guid? AffectedAccountId { get; init; }
    public bool? AppliesOnce { get; init; }
    public PaymentFrequency? RecurrenceFrequency { get; init; }
    public DateOnly? RecurrenceEndDate { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsActive { get; init; }
}

// === Projection DTOs ===

public record RunSimulationResponse
{
    public RunSimulationData Data { get; init; } = new();
    public SimulationMeta? Meta { get; init; }
}

public record RunSimulationData
{
    public Guid ScenarioId { get; init; }
    public string Status { get; init; } = "completed";
    public int PeriodsCalculated { get; init; }
    public long ExecutionTimeMs { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public List<MilestoneResult> KeyMilestones { get; init; } = new();
}

public record MilestoneResult
{
    public string Description { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public decimal? Value { get; init; }
    public decimal? YearsAway { get; init; }
}

public record ProjectionResponse
{
    public ProjectionData Data { get; init; } = new();
    public ProjectionMeta? Meta { get; init; }
}

public record ProjectionData
{
    public Guid ScenarioId { get; init; }
    public List<MonthlyProjection> MonthlyProjections { get; init; } = new();
    public List<MilestoneResult> Milestones { get; init; } = new();
    public ProjectionSummary Summary { get; init; } = new();
}

public record MonthlyProjection
{
    public string Period { get; init; } = string.Empty; // YYYY-MM format
    public decimal NetWorth { get; init; }
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public Dictionary<string, decimal>? BreakdownByType { get; init; }
    public Dictionary<string, decimal>? BreakdownByCurrency { get; init; }
    public List<AccountProjectionItem>? Accounts { get; init; }
}

public record AccountProjectionItem
{
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal BalanceHomeCurrency { get; init; }
    public decimal PeriodIncome { get; init; }
    public decimal PeriodExpenses { get; init; }
    public decimal PeriodInterest { get; init; }
}

public record ProjectionSummary
{
    public decimal StartNetWorth { get; init; }
    public decimal EndNetWorth { get; init; }
    public decimal TotalGrowth { get; init; }
    public decimal AnnualizedReturn { get; init; }
    public decimal AvgMonthlyGrowthRate { get; init; } // Average month-over-month growth rate
    public int TotalMonths { get; init; }
}

public record ProjectionMeta
{
    public Guid ScenarioId { get; init; }
    public string Granularity { get; init; } = "monthly";
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
