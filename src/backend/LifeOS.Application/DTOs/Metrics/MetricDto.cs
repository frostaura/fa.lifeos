namespace LifeOS.Application.DTOs.Metrics;

// Record Metrics Request/Response
public record RecordMetricsRequest
{
    public DateTime? Timestamp { get; init; }
    public string Source { get; init; } = "manual";
    public Dictionary<string, decimal?> Metrics { get; init; } = new();
}

public record RecordMetricsResponse
{
    public RecordMetricsData Data { get; init; } = new();
}

public record RecordMetricsData
{
    public string Type { get; init; } = "metricRecordBatch";
    public RecordMetricsAttributes Attributes { get; init; } = new();
    public List<MetricRecordResult> Records { get; init; } = new();
}

public record RecordMetricsAttributes
{
    public int Recorded { get; init; }
    public int Failed { get; init; }
    public DateTime Timestamp { get; init; }
    public string Source { get; init; } = string.Empty;
}

public record MetricRecordResult
{
    public string Code { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? Id { get; init; }
    public string? Error { get; init; }
}

// Metric History Response
public record MetricHistoryResponse
{
    public Dictionary<string, MetricHistoryData> Data { get; init; } = new();
    public MetricHistoryMeta Meta { get; init; } = new();
}

public record MetricHistoryData
{
    public List<MetricHistoryPoint> Points { get; init; } = new();
    public decimal? TargetValue { get; init; }
}

public record MetricHistoryPoint
{
    public DateTime Timestamp { get; init; }
    public decimal Value { get; init; }
    public string? Source { get; init; }
    public string? Aggregation { get; init; }
}

public record MetricHistoryMeta
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string Granularity { get; init; } = "raw";
    public List<string> MetricsReturned { get; init; } = new();
}

// Metric Definitions Response
public record MetricDefinitionListResponse
{
    public List<MetricDefinitionItemResponse> Data { get; init; } = new();
}

public record MetricDefinitionItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "metricDefinition";
    public MetricDefinitionAttributes Attributes { get; init; } = new();
}

public record MetricDefinitionAttributes
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? DimensionId { get; init; }
    public string? DimensionCode { get; init; }
    public string? Unit { get; init; }
    public string ValueType { get; init; } = "number";
    public string AggregationType { get; init; } = "last";
    public string[]? EnumValues { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public decimal? TargetValue { get; init; }
    public string TargetDirection { get; init; } = "atOrAbove";
    public string? Icon { get; init; }
    public string[]? Tags { get; init; }
    public bool IsDerived { get; init; }
    public string? DerivationFormula { get; init; }
    public bool IsActive { get; init; }
    public decimal? LatestValue { get; init; }
    public DateTime? LatestRecordedAt { get; init; }
}

public record MetricDefinitionDetailResponse
{
    public MetricDefinitionItemResponse Data { get; init; } = new();
}

// Create Metric Definition Request
public record CreateMetricDefinitionRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? DimensionId { get; init; }
    public string? Unit { get; init; }
    public string ValueType { get; init; } = "number";
    public string AggregationType { get; init; } = "last";
    public string[]? EnumValues { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public decimal? TargetValue { get; init; }
    public string TargetDirection { get; init; } = "atOrAbove";
    public string? Icon { get; init; }
    public string[]? Tags { get; init; }
    public bool IsDerived { get; init; }
    public string? DerivationFormula { get; init; }
    public bool IsActive { get; init; } = true;
}

// Update Metric Definition Request
public record UpdateMetricDefinitionRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public Guid? DimensionId { get; init; }
    public string? Unit { get; init; }
    public string? ValueType { get; init; }
    public string? AggregationType { get; init; }
    public string[]? EnumValues { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public decimal? TargetValue { get; init; }
    public string? TargetDirection { get; init; }
    public string? Icon { get; init; }
    public string[]? Tags { get; init; }
    public bool? IsDerived { get; init; }
    public string? DerivationFormula { get; init; }
    public bool? IsActive { get; init; }
}

// Metric Records List Response
public record MetricRecordListResponse
{
    public List<MetricRecordItemResponse> Data { get; init; } = new();
    public MetricRecordListMeta? Meta { get; init; }
    public LifeOS.Application.DTOs.Common.PaginationLinks? Links { get; init; }
}

public record MetricRecordItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "metricRecord";
    public MetricRecordAttributes Attributes { get; init; } = new();
}

public record MetricRecordAttributes
{
    public string MetricCode { get; init; } = string.Empty;
    public decimal? ValueNumber { get; init; }
    public bool? ValueBoolean { get; init; }
    public string? ValueString { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? Metadata { get; init; }
}

public record MetricRecordListMeta
{
    public int Page { get; init; }
    public int PerPage { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record MetricRecordDetailResponse
{
    public MetricRecordItemResponse Data { get; init; } = new();
}

// Update Metric Record Request
public record UpdateMetricRecordRequest
{
    public decimal? ValueNumber { get; init; }
    public bool? ValueBoolean { get; init; }
    public string? ValueString { get; init; }
    public string? Notes { get; init; }
    public string? Metadata { get; init; }
}

// v3.0: Nested Metrics Ingestion Request
public record MetricRecordRequest
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Source { get; init; } = "api";
    public Dictionary<string, object> Metrics { get; init; } = new();
}

// v3.0: Nested Metrics Ingestion Response
public record MetricRecordResponse
{
    public bool Success { get; init; }
    public int CreatedRecords { get; init; }
    public List<string> IgnoredMetrics { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}
