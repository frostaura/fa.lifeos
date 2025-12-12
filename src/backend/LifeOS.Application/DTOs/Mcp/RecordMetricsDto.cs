using System.ComponentModel;

namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// Internal DTO used by handlers for metric processing.
/// </summary>
public class RecordMetricsRequestDto
{
    public DateTime? Timestamp { get; set; }
    public string Source { get; set; } = "ai_assistant";
    public Dictionary<string, object> Metrics { get; set; } = new();
}

#region Record Metrics

/// <summary>
/// Request to record multiple metrics.
/// </summary>
public class RecordMetricsRequest
{
    [Description("Source identifier for where this data originated (e.g., 'ai_assistant', 'manual', 'fitbit')")]
    public string Source { get; set; } = "ai_assistant";

    [Description("Array of metric entries to record")]
    public List<MetricEntry> Entries { get; set; } = new();
}

/// <summary>
/// A single metric entry for recording.
/// </summary>
public class MetricEntry
{
    [Description("The unique metric code identifying which metric to record (use listMetrics to get available codes)")]
    public string Code { get; set; } = string.Empty;

    [Description("The numeric value to record for number-type metrics")]
    public decimal? ValueNumber { get; set; }

    [Description("The boolean value to record for boolean-type metrics")]
    public bool? ValueBoolean { get; set; }

    [Description("The string value to record for string or enum-type metrics")]
    public string? ValueString { get; set; }

    [Description("Notes providing context about this recording")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Response from recording metrics.
/// </summary>
public class RecordMetricsResponse
{
    [Description("Number of metric records successfully created")]
    public int CreatedCount { get; set; }

    [Description("List of metric codes that were skipped")]
    public List<string> SkippedMetrics { get; set; } = new();

    [Description("List of any errors encountered")]
    public List<string> Errors { get; set; } = new();

    [Description("Details for each processed metric")]
    public List<RecordedMetricDetail> Details { get; set; } = new();
}

/// <summary>
/// Details about a recorded metric.
/// </summary>
public class RecordedMetricDetail
{
    [Description("The metric code that was processed")]
    public string Code { get; set; } = string.Empty;

    [Description("Whether the recording was successful")]
    public bool Success { get; set; }

    [Description("ID of the created metric record")]
    public Guid? RecordId { get; set; }

    [Description("Error message if recording failed")]
    public string Error { get; set; } = string.Empty;
}

#endregion

#region List Metrics

/// <summary>
/// Request to list available metric definitions.
/// </summary>
public class ListMetricsRequest
{
    [Description("Filter metrics by dimension code (e.g., 'health', 'wealth', 'wisdom')")]
    public string DimensionCode { get; set; } = string.Empty;

    [Description("When true, only returns active metrics")]
    public bool OnlyActive { get; set; } = true;

    [Description("When true, includes the most recent recorded value for each metric")]
    public bool IncludeLatestValue { get; set; } = true;
}

/// <summary>
/// Response containing available metric definitions.
/// </summary>
public class ListMetricsResponse
{
    [Description("Array of metric definitions")]
    public List<MetricDefinitionSummary> Metrics { get; set; } = new();

    [Description("Total number of metrics returned")]
    public int TotalCount { get; set; }

    [Description("Available dimension codes for filtering")]
    public List<string> AvailableDimensions { get; set; } = new();
}

/// <summary>
/// Summary of a metric definition.
/// </summary>
public class MetricDefinitionSummary
{
    [Description("Unique identifier for the metric")]
    public Guid Id { get; set; }

    [Description("Metric code used for recording (e.g., 'weight_kg', 'steps_count')")]
    public string Code { get; set; } = string.Empty;

    [Description("Display name for the metric")]
    public string Name { get; set; } = string.Empty;

    [Description("Description of what this metric measures")]
    public string Description { get; set; } = string.Empty;

    [Description("Dimension this metric belongs to")]
    public string DimensionCode { get; set; } = string.Empty;

    [Description("Unit of measurement (e.g., 'kg', 'steps', '%')")]
    public string Unit { get; set; } = string.Empty;

    [Description("Value type: 'number', 'boolean', 'string', 'enum'")]
    public string ValueType { get; set; } = "number";

    [Description("Aggregation type: 'last', 'sum', 'avg', 'min', 'max'")]
    public string AggregationType { get; set; } = "last";

    [Description("Valid enum values for enum-type metrics")]
    public List<string> EnumValues { get; set; } = new();

    [Description("Minimum allowed value")]
    public decimal? MinValue { get; set; }

    [Description("Maximum allowed value")]
    public decimal? MaxValue { get; set; }

    [Description("Target/goal value")]
    public decimal? TargetValue { get; set; }

    [Description("Target direction: 'atOrAbove', 'atOrBelow', 'range'")]
    public string TargetDirection { get; set; } = "atOrAbove";

    [Description("Weight for health index calculation")]
    public decimal Weight { get; set; }

    [Description("Whether the metric is active")]
    public bool IsActive { get; set; }

    [Description("Most recent recorded value")]
    public decimal? LatestValue { get; set; }

    [Description("When the latest value was recorded")]
    public DateTime? LatestRecordedAt { get; set; }
}

#endregion

#region Get Metric History

/// <summary>
/// Request to get metric history.
/// </summary>
public class GetMetricHistoryRequest
{
    [Description("Metric codes to retrieve history for (comma-separated)")]
    public string MetricCodes { get; set; } = string.Empty;

    [Description("Start date for history range")]
    public DateTime? FromDate { get; set; }

    [Description("End date for history range")]
    public DateTime? ToDate { get; set; }

    [Description("Aggregation granularity: 'raw', 'daily', 'weekly', 'monthly'")]
    public string Granularity { get; set; } = "raw";

    [Description("Maximum number of data points to return")]
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Response containing metric history.
/// </summary>
public class GetMetricHistoryResponse
{
    [Description("History data for each requested metric")]
    public Dictionary<string, MetricHistoryData> MetricData { get; set; } = new();
}

/// <summary>
/// History data for a single metric.
/// </summary>
public class MetricHistoryData
{
    [Description("Metric code")]
    public string Code { get; set; } = string.Empty;

    [Description("Data points")]
    public List<MetricDataPoint> DataPoints { get; set; } = new();

    [Description("Target value for comparison")]
    public decimal? TargetValue { get; set; }

    [Description("Minimum value in the data")]
    public decimal? MinValue { get; set; }

    [Description("Maximum value in the data")]
    public decimal? MaxValue { get; set; }

    [Description("Average value in the data")]
    public decimal? AverageValue { get; set; }
}

/// <summary>
/// A single data point in metric history.
/// </summary>
public class MetricDataPoint
{
    [Description("Timestamp of the data point")]
    public DateTime Timestamp { get; set; }

    [Description("Value at this point")]
    public decimal Value { get; set; }

    [Description("Source of this recording")]
    public string Source { get; set; } = string.Empty;
}

#endregion

// Legacy DTOs for backward compatibility (deprecated, use new DTOs above)
public class MetricEntryDto
{
    [Description("The metric code to record")]
    public string Code { get; set; } = string.Empty;

    [Description("Numeric value for number-type metrics")]
    public decimal? ValueNumber { get; set; }

    [Description("Boolean value for boolean-type metrics")]
    public bool? ValueBoolean { get; set; }

    [Description("String value for string/enum-type metrics")]
    public string? ValueString { get; set; }

    [Description("Optional notes about the recording")]
    public string? Notes { get; set; }
}

/// <summary>
/// Response from recording metrics via MCP tool (legacy format).
/// </summary>
public class RecordMetricsResponseDto
{
    [Description("Number of metric records successfully created")]
    public int CreatedRecords { get; set; }

    [Description("List of metric codes that were skipped")]
    public List<string> IgnoredMetrics { get; set; } = new();

    [Description("List of error messages for any failures")]
    public List<string> Errors { get; set; } = new();

    [Description("Detailed results for each metric processed")]
    public List<RecordedMetricDetailDto> RecordedDetails { get; set; } = new();
}

/// <summary>
/// Details about a single recorded metric (legacy format).
/// </summary>
public class RecordedMetricDetailDto
{
    [Description("The metric code that was processed")]
    public string Code { get; set; } = string.Empty;

    [Description("Whether the recording was successful")]
    public bool Success { get; set; }

    [Description("ID of the created metric record")]
    public Guid? RecordId { get; set; }

    [Description("Error message if recording failed")]
    public string? Error { get; set; }
}
