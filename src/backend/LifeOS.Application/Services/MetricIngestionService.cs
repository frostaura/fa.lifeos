using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Services;

public class MetricIngestionService : IMetricIngestionService
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<MetricIngestionService> _logger;

    public MetricIngestionService(ILifeOSDbContext context, ILogger<MetricIngestionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MetricRecordResponse> ProcessNestedMetrics(
        MetricRecordRequest request, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var createdRecords = 0;
        var ignoredMetrics = new List<string>();
        var errors = new List<string>();

        try
        {
            // Load dimension codes for validation
            var dimensionCodes = await _context.Dimensions
                .Where(d => d.IsActive)
                .Select(d => d.Code)
                .ToListAsync(cancellationToken);

            var dimensionCodeSet = new HashSet<string>(dimensionCodes, StringComparer.OrdinalIgnoreCase);

            // Load metric definitions for validation
            var metricDefinitions = await _context.MetricDefinitions
                .Where(m => m.IsActive)
                .ToDictionaryAsync(m => m.Code, cancellationToken);

            // Flatten nested structure
            var flatMetrics = FlattenMetrics(request.Metrics, dimensionCodeSet, ignoredMetrics, errors);

            // Process each metric
            foreach (var (metricCode, value) in flatMetrics)
            {
                // Skip null values
                if (value == null)
                {
                    ignoredMetrics.Add(metricCode);
                    continue;
                }

                // Validate metric code exists
                if (!metricDefinitions.TryGetValue(metricCode, out var metricDef))
                {
                    errors.Add($"Unknown metric code: {metricCode}");
                    continue;
                }

                // Parse and validate value
                var (numberValue, boolValue, parseError) = ParseValue(value, metricCode, metricDef);
                
                if (parseError != null)
                {
                    errors.Add(parseError);
                    continue;
                }

                // Validate numeric bounds
                if (numberValue.HasValue)
                {
                    if (metricDef.MinValue.HasValue && numberValue.Value < metricDef.MinValue.Value)
                    {
                        errors.Add($"{metricCode}: Value {numberValue.Value} below minimum {metricDef.MinValue.Value}");
                        continue;
                    }

                    if (metricDef.MaxValue.HasValue && numberValue.Value > metricDef.MaxValue.Value)
                    {
                        errors.Add($"{metricCode}: Value {numberValue.Value} above maximum {metricDef.MaxValue.Value}");
                        continue;
                    }
                }

                // Create metric record
                var record = new MetricRecord
                {
                    UserId = userId,
                    MetricCode = metricCode,
                    ValueNumber = numberValue,
                    ValueBoolean = boolValue,
                    RecordedAt = request.Timestamp,
                    Source = request.Source
                };

                _context.MetricRecords.Add(record);
                createdRecords++;
            }

            // Save all records
            if (createdRecords > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created {Count} metric records for user {UserId} from source {Source}", 
                    createdRecords, userId, request.Source);
            }

            return new MetricRecordResponse
            {
                Success = errors.Count == 0 || createdRecords > 0,
                CreatedRecords = createdRecords,
                IgnoredMetrics = ignoredMetrics.Distinct().ToList(),
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing nested metrics for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Flattens nested dictionary structure into metric codes.
    /// Validates dimension codes and ignores invalid ones.
    /// Example: { "health_recovery": { "weight_kg": 82.5 } } => { "weight_kg": 82.5 }
    /// Example: { "asset_care": { "finance": { "net_worth_homeccy": 1250000 } } } => { "net_worth_homeccy": 1250000 }
    /// </summary>
    private Dictionary<string, object?> FlattenMetrics(
        Dictionary<string, object> input,
        HashSet<string> validDimensionCodes,
        List<string> ignoredMetrics,
        List<string> errors,
        string? currentPath = null)
    {
        var result = new Dictionary<string, object?>();

        foreach (var (key, value) in input)
        {
            var newPath = string.IsNullOrEmpty(currentPath) ? key : $"{currentPath}.{key}";

            // Check if this is a dimension code (first level only)
            if (currentPath == null && validDimensionCodes.Contains(key))
            {
                // This is a dimension, recurse into it
                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                {
                    var nested = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                    if (nested != null)
                    {
                        var flattened = FlattenMetrics(nested, validDimensionCodes, ignoredMetrics, errors, key);
                        foreach (var (flatKey, flatValue) in flattened)
                        {
                            result[flatKey] = flatValue;
                        }
                    }
                }
                else if (value is Dictionary<string, object> dict)
                {
                    var flattened = FlattenMetrics(dict, validDimensionCodes, ignoredMetrics, errors, key);
                    foreach (var (flatKey, flatValue) in flattened)
                    {
                        result[flatKey] = flatValue;
                    }
                }
                else
                {
                    errors.Add($"Dimension '{key}' must contain nested metric objects");
                }
            }
            // Check if this is potentially a category (nested object that's not a dimension)
            else if (value is JsonElement jsonEl && jsonEl.ValueKind == JsonValueKind.Object)
            {
                // Could be a category like "finance", recurse into it
                var nested = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonEl.GetRawText());
                if (nested != null)
                {
                    var flattened = FlattenMetrics(nested, validDimensionCodes, ignoredMetrics, errors, newPath);
                    foreach (var (flatKey, flatValue) in flattened)
                    {
                        result[flatKey] = flatValue;
                    }
                }
            }
            else if (value is Dictionary<string, object> dictValue)
            {
                // Could be a category, recurse into it
                var flattened = FlattenMetrics(dictValue, validDimensionCodes, ignoredMetrics, errors, newPath);
                foreach (var (flatKey, flatValue) in flattened)
                {
                    result[flatKey] = flatValue;
                }
            }
            else
            {
                // This is a leaf value (metric)
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Parses value from various types into metric values
    /// </summary>
    private (decimal? numberValue, bool? boolValue, string? error) ParseValue(
        object? value, 
        string metricCode,
        MetricDefinition metricDef)
    {
        if (value == null)
        {
            return (null, null, null);
        }

        try
        {
            decimal? numberValue = null;
            bool? boolValue = null;

            if (value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.Number:
                        numberValue = jsonElement.GetDecimal();
                        break;
                    case JsonValueKind.True:
                        boolValue = true;
                        break;
                    case JsonValueKind.False:
                        boolValue = false;
                        break;
                    case JsonValueKind.Null:
                        return (null, null, null);
                    default:
                        return (null, null, $"{metricCode}: Unsupported value type {jsonElement.ValueKind}");
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
            else if (value is float floatVal)
            {
                numberValue = (decimal)floatVal;
            }
            else if (value is bool boolVal)
            {
                boolValue = boolVal;
            }
            else
            {
                return (null, null, $"{metricCode}: Cannot parse value of type {value.GetType().Name}");
            }

            // Validate value type matches metric definition
            if (metricDef.ValueType == MetricValueType.Number && !numberValue.HasValue && !boolValue.HasValue)
            {
                return (null, null, $"{metricCode}: Expected numeric value for Number type metric");
            }

            if (metricDef.ValueType == MetricValueType.Boolean && !boolValue.HasValue)
            {
                return (null, null, $"{metricCode}: Expected boolean value for Boolean type metric");
            }

            return (numberValue, boolValue, null);
        }
        catch (Exception ex)
        {
            return (null, null, $"{metricCode}: Failed to parse value - {ex.Message}");
        }
    }
}
