using LifeOS.Application.Commands.Metrics;
using LifeOS.Application.DTOs.Metrics;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Queries.Metrics;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMetricIngestionService _metricIngestionService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IMediator mediator, 
        IMetricIngestionService metricIngestionService,
        ILogger<MetricsController> logger)
    {
        _mediator = mediator;
        _metricIngestionService = metricIngestionService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #region Metric Definitions CRUD

    /// <summary>
    /// Get all metric definitions
    /// </summary>
    [HttpGet("definitions")]
    [ProducesResponseType(typeof(MetricDefinitionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefinitions(
        [FromQuery] Guid? dimensionId,
        [FromQuery] string? tags,
        [FromQuery] bool? isActive = true)
    {
        var tagArray = string.IsNullOrEmpty(tags) ? null : tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        var result = await _mediator.Send(new GetMetricDefinitionsQuery(dimensionId, tagArray, isActive));
        return Ok(result);
    }

    /// <summary>
    /// Get single metric definition by code
    /// </summary>
    [HttpGet("definitions/{code}")]
    [ProducesResponseType(typeof(MetricDefinitionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDefinitionByCode(string code)
    {
        var result = await _mediator.Send(new GetMetricDefinitionByCodeQuery(code));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Metric definition not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create new metric definition
    /// </summary>
    [HttpPost("definitions")]
    [ProducesResponseType(typeof(MetricDefinitionDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateMetricDefinitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return UnprocessableEntity(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Code and Name are required"
                }
            });
        }

        try
        {
            if (!Enum.TryParse<MetricValueType>(request.ValueType, true, out var valueType))
                valueType = MetricValueType.Number;

            if (!Enum.TryParse<AggregationType>(request.AggregationType, true, out var aggregationType))
                aggregationType = AggregationType.Last;

            if (!Enum.TryParse<TargetDirection>(request.TargetDirection, true, out var targetDirection))
                targetDirection = TargetDirection.AtOrAbove;

            var result = await _mediator.Send(new CreateMetricDefinitionCommand(
                request.Code,
                request.Name,
                request.Description,
                request.DimensionId,
                request.Unit,
                valueType,
                aggregationType,
                request.EnumValues,
                request.MinValue,
                request.MaxValue,
                request.TargetValue,
                targetDirection,
                request.Icon,
                request.Tags,
                request.IsDerived,
                request.DerivationFormula,
                request.IsActive));

            return CreatedAtAction(nameof(GetDefinitionByCode), new { code = result.Data.Attributes.Code }, result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new
            {
                error = new
                {
                    code = "DUPLICATE_CODE",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Update metric definition
    /// </summary>
    [HttpPatch("definitions/{code}")]
    [ProducesResponseType(typeof(MetricDefinitionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDefinition(string code, [FromBody] UpdateMetricDefinitionRequest request)
    {
        MetricValueType? valueType = null;
        if (!string.IsNullOrEmpty(request.ValueType) && Enum.TryParse<MetricValueType>(request.ValueType, true, out var vt))
            valueType = vt;

        AggregationType? aggregationType = null;
        if (!string.IsNullOrEmpty(request.AggregationType) && Enum.TryParse<AggregationType>(request.AggregationType, true, out var at))
            aggregationType = at;

        TargetDirection? targetDirection = null;
        if (!string.IsNullOrEmpty(request.TargetDirection) && Enum.TryParse<TargetDirection>(request.TargetDirection, true, out var td))
            targetDirection = td;

        var success = await _mediator.Send(new UpdateMetricDefinitionCommand(
            code,
            request.Name,
            request.Description,
            request.DimensionId,
            request.Unit,
            valueType,
            aggregationType,
            request.EnumValues,
            request.MinValue,
            request.MaxValue,
            request.TargetValue,
            targetDirection,
            request.Icon,
            request.Tags,
            request.IsDerived,
            request.DerivationFormula,
            request.IsActive));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Metric definition not found" } });

        var result = await _mediator.Send(new GetMetricDefinitionByCodeQuery(code));
        return Ok(result);
    }

    /// <summary>
    /// Delete metric definition (soft delete)
    /// </summary>
    [HttpDelete("definitions/{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDefinition(string code)
    {
        var success = await _mediator.Send(new DeleteMetricDefinitionCommand(code));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Metric definition not found" } });

        return NoContent();
    }

    #endregion

    #region Metric Records CRUD

    /// <summary>
    /// List metric records with pagination
    /// </summary>
    [HttpGet("records")]
    [ProducesResponseType(typeof(MetricRecordListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecords(
        [FromQuery] string? code,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMetricRecordsQuery(
            GetUserId(),
            code,
            from,
            to,
            page,
            Math.Min(pageSize, 100)));

        return Ok(result);
    }

    /// <summary>
    /// Get single metric record by id
    /// </summary>
    [HttpGet("records/{id:guid}")]
    [ProducesResponseType(typeof(MetricRecordDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecordById(Guid id)
    {
        var result = await _mediator.Send(new GetMetricRecordByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Metric record not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Update metric record value
    /// </summary>
    [HttpPatch("records/{id:guid}")]
    [ProducesResponseType(typeof(MetricRecordDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRecord(Guid id, [FromBody] UpdateMetricRecordRequest request)
    {
        var success = await _mediator.Send(new UpdateMetricRecordCommand(
            GetUserId(),
            id,
            request.ValueNumber,
            request.ValueBoolean,
            request.ValueString,
            request.Notes,
            request.Metadata));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Metric record not found" } });

        var result = await _mediator.Send(new GetMetricRecordByIdQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Delete metric record
    /// </summary>
    [HttpDelete("records/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecord(Guid id)
    {
        var success = await _mediator.Send(new DeleteMetricRecordCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Metric record not found" } });

        return NoContent();
    }

    #endregion

    #region Existing Endpoints

    /// <summary>
    /// Record one or more metric values (v3.0: Supports both flat and nested structures)
    /// Flat: { "metrics": { "weight_kg": 74.5, "steps_count": 10000 } }
    /// Nested: { "metrics": { "health_recovery": { "weight_kg": 74.5 }, "asset_care": { "finance": { "net_worth_homeccy": 1250000 } } } }
    /// </summary>
    [HttpPost("record")]
    [EnableRateLimiting("metrics")]
    [ProducesResponseType(typeof(MetricRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordMetrics([FromBody] MetricRecordRequest request)
    {
        if (request.Metrics == null || !request.Metrics.Any())
        {
            return UnprocessableEntity(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "At least one metric must be provided"
                }
            });
        }

        try
        {
            var result = await _metricIngestionService.ProcessNestedMetrics(
                request,
                GetUserId(),
                HttpContext.RequestAborted);

            _logger.LogInformation("Recorded {Created} metrics from source {Source} (ignored: {Ignored}, errors: {Errors})", 
                result.CreatedRecords, 
                request.Source,
                result.IgnoredMetrics.Count,
                result.Errors.Count);

            if (!result.Success && result.CreatedRecords == 0)
            {
                return UnprocessableEntity(new
                {
                    error = new
                    {
                        code = "VALIDATION_ERROR",
                        message = "Failed to record metrics",
                        details = result.Errors
                    }
                });
            }

            // Return response in format expected by frontend
            return StatusCode(201, new
            {
                data = new
                {
                    type = "metricRecordBatch",
                    attributes = new
                    {
                        recorded = result.CreatedRecords,
                        failed = result.Errors.Count,
                        timestamp = request.Timestamp,
                        source = request.Source
                    },
                    records = new List<object>() // Empty for now, can be populated if needed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metrics for user {UserId}", GetUserId());
            return StatusCode(500, new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "An error occurred while recording metrics"
                }
            });
        }
    }

    /// <summary>
    /// Get metric history with aggregation
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(MetricHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string codes,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string granularity = "raw",
        [FromQuery] int limit = 100)
    {
        if (string.IsNullOrEmpty(codes))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "codes parameter is required"
                }
            });
        }

        var codeArray = codes.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        var result = await _mediator.Send(new GetMetricHistoryQuery(
            GetUserId(),
            codeArray,
            from,
            to,
            granularity,
            Math.Min(limit, 1000)));

        return Ok(result);
    }

    /// <summary>
    /// v1.1: Record metrics using nested payload structure
    /// Accepts: { "metrics": { "health_recovery": { "weight_kg": 74.5, ... }, "asset_care": { "finance": { "net_worth_homeccy": 1250000 } } } }
    /// </summary>
    [HttpPost("record/nested")]
    [EnableRateLimiting("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordNestedMetrics(
        [FromBody] NestedMetricsRequest request,
        [FromQuery] bool allowDynamicCreation = false)
    {
        if (request.Metrics == null || !request.Metrics.Any())
        {
            return UnprocessableEntity(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Metrics object is required"
                }
            });
        }

        var userId = GetUserId();
        var timestamp = request.Timestamp ?? DateTime.UtcNow;
        var source = request.Source ?? "nested_api";
        
        var flatMetrics = new Dictionary<string, decimal?>();
        var errors = new List<object>();
        var skippedCount = 0;

        // Flatten nested structure
        foreach (var (dimensionCode, dimensionMetrics) in request.Metrics)
        {
            if (dimensionMetrics is System.Text.Json.JsonElement jsonElement)
            {
                FlattenJsonElement(dimensionCode, jsonElement, flatMetrics, errors, ref skippedCount);
            }
        }

        if (!flatMetrics.Any() && errors.Any())
        {
            return UnprocessableEntity(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "No valid metrics found",
                    details = errors
                }
            });
        }

        // Record the flattened metrics using existing command
        var result = await _mediator.Send(new RecordMetricsCommand(
            userId,
            timestamp,
            source,
            flatMetrics));

        _logger.LogInformation("Recorded {Recorded} nested metrics from source {Source}", 
            result.Data.Attributes.Recorded, 
            source);

        return Ok(new
        {
            data = new
            {
                recordedCount = result.Data.Attributes.Recorded,
                skippedCount,
                errors
            },
            meta = new
            {
                timestamp = DateTime.UtcNow,
                processingTimeMs = 0
            }
        });
    }

    private void FlattenJsonElement(
        string path, 
        System.Text.Json.JsonElement element, 
        Dictionary<string, decimal?> metrics,
        List<object> errors,
        ref int skippedCount)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var newPath = $"{path}.{prop.Name}";
                FlattenJsonElement(newPath, prop.Value, metrics, errors, ref skippedCount);
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            // Extract metric code from path (last segment)
            var segments = path.Split('.');
            var metricCode = segments.Length > 1 ? segments[^1] : path;
            metrics[metricCode] = element.GetDecimal();
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Null)
        {
            skippedCount++;
        }
        else
        {
            // Non-numeric values are logged as errors for this numeric-only endpoint
            var segments = path.Split('.');
            var metricCode = segments.Length > 1 ? segments[^1] : path;
            errors.Add(new { code = metricCode, error = "Only numeric values supported in nested endpoint" });
        }
    }

    #endregion
}

/// <summary>
/// v1.1: Nested metrics ingestion request
/// </summary>
public record NestedMetricsRequest
{
    public DateTime? Timestamp { get; init; }
    public string? Source { get; init; }
    public Dictionary<string, object>? Metrics { get; init; }
}
