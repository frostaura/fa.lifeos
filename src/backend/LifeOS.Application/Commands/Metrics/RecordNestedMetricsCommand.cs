using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Metrics;

/// <summary>
/// v1.2: Nested metrics ingestion command
/// Accepts hierarchical structure: { "dimension": { "metric": value, "nested": { "metric2": value } } }
/// </summary>
public record RecordNestedMetricsCommand(
    Guid UserId,
    DateTime? Timestamp,
    string Source,
    Dictionary<string, object> Metrics
) : IRequest<RecordNestedMetricsResponse>;

public class RecordNestedMetricsCommandHandler : IRequestHandler<RecordNestedMetricsCommand, RecordNestedMetricsResponse>
{
    private readonly ILifeOSDbContext _context;

    public RecordNestedMetricsCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<RecordNestedMetricsResponse> Handle(RecordNestedMetricsCommand request, CancellationToken cancellationToken)
    {
        var timestamp = request.Timestamp ?? DateTime.UtcNow;
        var createdRecords = new List<Guid>();
        var ignoredMetrics = new List<string>();
        var errors = new List<string>();

        // Get all active metric definitions
        var validMetrics = await _context.MetricDefinitions
            .Where(m => m.IsActive)
            .ToDictionaryAsync(m => m.Code, cancellationToken);

        // Flatten nested structure
        var flatMetrics = FlattenMetrics(request.Metrics);

        foreach (var (code, value) in flatMetrics)
        {
            // Skip null values
            if (value == null)
            {
                ignoredMetrics.Add(code);
                continue;
            }

            // Check if metric exists
            if (!validMetrics.TryGetValue(code, out var metricDef))
            {
                ignoredMetrics.Add(code);
                continue;
            }

            // Parse value based on metric type
            decimal? numberValue = null;
            bool? boolValue = null;

            try
            {
                if (value is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Number)
                    {
                        numberValue = jsonElement.GetDecimal();
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.True)
                    {
                        boolValue = true;
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.False)
                    {
                        boolValue = false;
                    }
                }
                else if (value is decimal dec)
                {
                    numberValue = dec;
                }
                else if (value is double dbl)
                {
                    numberValue = (decimal)dbl;
                }
                else if (value is int intVal)
                {
                    numberValue = intVal;
                }
                else if (value is long longVal)
                {
                    numberValue = longVal;
                }
                else if (value is bool boolVal)
                {
                    boolValue = boolVal;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to parse value for {code}: {ex.Message}");
                continue;
            }

            // Validate bounds for numeric metrics
            if (numberValue.HasValue)
            {
                if (metricDef.MinValue.HasValue && numberValue.Value < metricDef.MinValue.Value)
                {
                    errors.Add($"{code}: Value {numberValue.Value} below minimum {metricDef.MinValue.Value}");
                    continue;
                }

                if (metricDef.MaxValue.HasValue && numberValue.Value > metricDef.MaxValue.Value)
                {
                    errors.Add($"{code}: Value {numberValue.Value} above maximum {metricDef.MaxValue.Value}");
                    continue;
                }
            }

            // Create metric record
            var record = new MetricRecord
            {
                UserId = request.UserId,
                MetricCode = code,
                ValueNumber = numberValue,
                ValueBoolean = boolValue,
                RecordedAt = timestamp,
                Source = request.Source
            };

            _context.MetricRecords.Add(record);
            createdRecords.Add(record.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new RecordNestedMetricsResponse
        {
            Success = errors.Count == 0 || createdRecords.Count > 0,
            CreatedRecords = createdRecords.Count,
            IgnoredMetrics = ignoredMetrics,
            Errors = errors
        };
    }

    /// <summary>
    /// Flattens nested dictionary structure into flat metric codes.
    /// Example: { "health_recovery": { "weight_kg": 82.5 } } => { "weight_kg": 82.5 }
    /// Or with prefix: { "health_recovery": { "pet_care": { "cat_brushing": 1 } } } => { "cat_brushing": 1 } or { "pet_care.cat_brushing": 1 }
    /// </summary>
    private Dictionary<string, object?> FlattenMetrics(Dictionary<string, object> input, string? prefix = null)
    {
        var result = new Dictionary<string, object?>();

        foreach (var (key, value) in input)
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Recursively flatten nested objects
                var nested = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                if (nested != null)
                {
                    var flattened = FlattenMetrics(nested, key);
                    foreach (var (flatKey, flatValue) in flattened)
                    {
                        result[flatKey] = flatValue;
                    }
                }
            }
            else if (value is Dictionary<string, object> dict)
            {
                // Recursively flatten nested dictionaries
                var flattened = FlattenMetrics(dict, key);
                foreach (var (flatKey, flatValue) in flattened)
                {
                    result[flatKey] = flatValue;
                }
            }
            else
            {
                // Leaf value - use just the key (ignore dimension prefix)
                result[key] = value;
            }
        }

        return result;
    }
}

public class RecordNestedMetricsResponse
{
    public bool Success { get; set; }
    public int CreatedRecords { get; set; }
    public List<string> IgnoredMetrics { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
