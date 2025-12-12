using System.ComponentModel;

namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// MCP Tool: lifeos.listMetrics
/// Lists available metric definitions with their configuration and latest values.
/// </summary>
public class ListMetricsRequestDto
{
    /// <summary>Optional: Filter metrics by dimension code (e.g., "health", "wealth")</summary>
    [Description("Optional filter to return only metrics belonging to a specific dimension (e.g., 'health', 'wealth', 'wisdom')")]
    public string? DimensionCode { get; set; }

    /// <summary>Only include active metrics (default: true)</summary>
    [Description("When true, only returns metrics that are currently active. Set to false to include inactive/archived metrics.")]
    public bool OnlyActive { get; set; } = true;

    /// <summary>Include latest recorded value for each metric (default: true)</summary>
    [Description("When true, includes the most recent recorded value and timestamp for each metric. Disable to reduce response size.")]
    public bool IncludeLatestValue { get; set; } = true;

    /// <summary>Optional: Filter by specific tags</summary>
    [Description("Optional array of tags to filter metrics by (e.g., ['vital', 'body_composition']). Metrics matching ANY tag are returned.")]
    public string[]? Tags { get; set; }
}

/// <summary>
/// Response containing list of available metric definitions.
/// </summary>
public class ListMetricsResponseDto
{
    /// <summary>List of metric definitions matching the filter criteria</summary>
    [Description("Array of metric definitions that match the request filters")]
    public List<MetricDefinitionSummaryDto> Metrics { get; set; } = new();

    /// <summary>Total count of metrics returned</summary>
    [Description("Total number of metrics in the response")]
    public int TotalCount { get; set; }

    /// <summary>Available dimension codes for filtering</summary>
    [Description("List of all unique dimension codes that have metrics, useful for building filter UI")]
    public List<string> AvailableDimensions { get; set; } = new();
}

/// <summary>
/// Summary of a single metric definition for AI consumption.
/// </summary>
public class MetricDefinitionSummaryDto
{
    /// <summary>Metric unique identifier</summary>
    [Description("Unique GUID identifier for the metric definition")]
    public Guid Id { get; set; }

    /// <summary>Metric code used for recording (e.g., "weight_kg", "steps_count")</summary>
    [Description("The unique code used when recording metric values. Use this code as the key in the recordMetrics tool.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable metric name</summary>
    [Description("Display name for the metric (e.g., 'Body Weight', 'Daily Steps')")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of what this metric measures</summary>
    [Description("Detailed description explaining what this metric tracks and how it should be used")]
    public string? Description { get; set; }

    /// <summary>Dimension code this metric belongs to</summary>
    [Description("The dimension/category this metric belongs to (e.g., 'health', 'wealth', 'wisdom')")]
    public string? DimensionCode { get; set; }

    /// <summary>Unit of measurement</summary>
    [Description("Unit of measurement for the metric value (e.g., 'kg', 'steps', '%', 'bpm')")]
    public string? Unit { get; set; }

    /// <summary>Value type (number, boolean, string, enum)</summary>
    [Description("The data type for metric values: 'number' (decimal), 'boolean' (true/false), 'string' (text), or 'enum' (predefined options)")]
    public string ValueType { get; set; } = "number";

    /// <summary>How multiple values in a period are aggregated</summary>
    [Description("How values are aggregated when multiple records exist: 'last' (most recent), 'sum' (total), 'avg' (average), 'min', 'max'")]
    public string AggregationType { get; set; } = "last";

    /// <summary>Valid enum values if ValueType is enum</summary>
    [Description("For enum-type metrics, the list of valid string values that can be recorded")]
    public string[]? EnumValues { get; set; }

    /// <summary>Minimum allowed value</summary>
    [Description("The minimum valid value for this metric. Values below this may be rejected or flagged.")]
    public decimal? MinValue { get; set; }

    /// <summary>Maximum allowed value</summary>
    [Description("The maximum valid value for this metric. Values above this may be rejected or flagged.")]
    public decimal? MaxValue { get; set; }

    /// <summary>Target/goal value</summary>
    [Description("The user's target/goal value for this metric. Used for progress tracking and health index calculations.")]
    public decimal? TargetValue { get; set; }

    /// <summary>Direction for target comparison</summary>
    [Description("How to compare against target: 'atOrAbove' (higher is better), 'atOrBelow' (lower is better), 'range' (within min/max is optimal)")]
    public string TargetDirection { get; set; } = "atOrAbove";

    /// <summary>Weight for health index calculation (0-1)</summary>
    [Description("Relative importance weight for health index calculation. Higher weight = more impact on overall health score. Typically 0.10-0.30.")]
    public decimal Weight { get; set; }

    /// <summary>Icon identifier for UI</summary>
    [Description("Icon identifier or emoji for display purposes")]
    public string? Icon { get; set; }

    /// <summary>Tags for categorization</summary>
    [Description("Array of tags for categorization and filtering (e.g., ['vital', 'body_composition', 'cardiovascular'])")]
    public string[]? Tags { get; set; }

    /// <summary>Whether this is a derived/calculated metric</summary>
    [Description("True if this metric is calculated from other metrics rather than directly recorded")]
    public bool IsDerived { get; set; }

    /// <summary>Whether this metric is currently active</summary>
    [Description("True if the metric is active and should be tracked. Inactive metrics are archived.")]
    public bool IsActive { get; set; }

    /// <summary>Most recently recorded value</summary>
    [Description("The most recent recorded value for this metric (only included if IncludeLatestValue is true)")]
    public decimal? LatestValue { get; set; }

    /// <summary>Timestamp of the latest recorded value</summary>
    [Description("ISO 8601 timestamp of when the latest value was recorded")]
    public DateTime? LatestRecordedAt { get; set; }
}
