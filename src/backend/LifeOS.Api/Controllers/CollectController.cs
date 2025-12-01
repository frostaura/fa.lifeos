using System.Security.Claims;
using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/collect")]
[Authorize]
public class CollectController : ControllerBase
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<CollectController> _logger;

    public CollectController(ILifeOSDbContext context, ILogger<CollectController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Collect metrics from external sources (n8n, iOS Shortcuts, etc.)
    /// This is the primary endpoint for external integrations.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("metrics")]
    [ProducesResponseType(typeof(CollectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Collect([FromBody] CollectRequest request)
    {
        var userId = GetUserId();
        var apiKeyPrefix = HttpContext.Items["ApiKeyPrefix"]?.ToString();
        var isApiKeyAuth = HttpContext.Items["IsApiKeyAuthenticated"] as bool? ?? false;
        
        // Log the incoming event
        var eventLog = new ApiEventLog
        {
            UserId = userId,
            EventType = "metric_collect",
            Source = request.Source ?? "unknown",
            ApiKeyPrefix = apiKeyPrefix,
            RequestPayload = JsonSerializer.Serialize(request),
            Timestamp = DateTime.UtcNow,
            Status = "processing"
        };

        try
        {
            // Validate request
            if (request.Metrics == null || !request.Metrics.Any())
            {
                eventLog.Status = "error";
                eventLog.ErrorMessage = "No metrics provided";
                _context.ApiEventLogs.Add(eventLog);
                await _context.SaveChangesAsync(CancellationToken.None);

                return UnprocessableEntity(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "At least one metric must be provided in the 'metrics' array"
                });
            }

            var timestamp = request.Timestamp ?? DateTime.UtcNow;
            var recordedCount = 0;
            var errors = new List<MetricError>();

            // Get valid metric codes
            var validMetricCodes = _context.MetricDefinitions
                .Where(m => m.IsActive)
                .Select(m => m.Code)
                .ToHashSet();

            foreach (var metric in request.Metrics)
            {
                try
                {
                    // Validate metric code exists
                    if (!validMetricCodes.Contains(metric.Code))
                    {
                        errors.Add(new MetricError
                        {
                            Code = metric.Code,
                            Message = $"Unknown metric code: '{metric.Code}'. Valid codes are: {string.Join(", ", validMetricCodes.Take(10))}{(validMetricCodes.Count > 10 ? "..." : "")}"
                        });
                        continue;
                    }

                    var metricRecord = new MetricRecord
                    {
                        UserId = userId,
                        MetricCode = metric.Code,
                        ValueNumber = metric.Value,
                        RecordedAt = timestamp,
                        Source = request.Source ?? "api",
                        Metadata = metric.Metadata != null ? JsonSerializer.Serialize(metric.Metadata) : null
                    };

                    _context.MetricRecords.Add(metricRecord);
                    recordedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add(new MetricError
                    {
                        Code = metric.Code,
                        Message = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync(CancellationToken.None);

            eventLog.Status = "success";
            eventLog.ResponsePayload = JsonSerializer.Serialize(new { recorded = recordedCount, errors = errors.Count });
            _context.ApiEventLogs.Add(eventLog);
            await _context.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation("Collected {Count} metrics from source {Source} via {AuthType}", 
                recordedCount, 
                request.Source, 
                isApiKeyAuth ? "API key" : "JWT");

            return StatusCode(201, new CollectResponse
            {
                Success = true,
                Data = new CollectResponseData
                {
                    Recorded = recordedCount,
                    Errors = errors,
                    Timestamp = timestamp,
                    EventId = eventLog.Id
                }
            });
        }
        catch (Exception ex)
        {
            eventLog.Status = "error";
            eventLog.ErrorMessage = ex.Message;
            _context.ApiEventLogs.Add(eventLog);
            await _context.SaveChangesAsync(CancellationToken.None);

            _logger.LogError(ex, "Error collecting metrics");
            throw;
        }
    }

    /// <summary>
    /// Get recent API events/logs for the playground
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(EventLogResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int limit = 50,
        [FromQuery] string? eventType = null)
    {
        var userId = GetUserId();
        var query = _context.ApiEventLogs
            .Where(e => e.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        var events = await Task.FromResult(query
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .Select(e => new EventLogItem
            {
                Id = e.Id,
                EventType = e.EventType,
                Source = e.Source,
                Status = e.Status,
                Timestamp = e.Timestamp,
                RequestPayload = e.RequestPayload,
                ResponsePayload = e.ResponsePayload,
                ErrorMessage = e.ErrorMessage
            })
            .ToList());

        return Ok(new EventLogResponse
        {
            Events = events,
            Total = events.Count
        });
    }

    /// <summary>
    /// Get the JSON schema for the collect endpoint
    /// </summary>
    [HttpGet("schema")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetSchema()
    {
        var schema = new
        {
            type = "object",
            required = new[] { "metrics" },
            properties = new
            {
                source = new
                {
                    type = "string",
                    description = "Source of the metrics (e.g., 'n8n', 'ios_shortcuts', 'apple_health')",
                    examples = new[] { "n8n", "ios_shortcuts", "apple_health" }
                },
                timestamp = new
                {
                    type = "string",
                    format = "date-time",
                    description = "ISO 8601 timestamp for when the metrics were captured. Defaults to current time if not provided."
                },
                metrics = new
                {
                    type = "array",
                    minItems = 1,
                    description = "Array of metrics to record",
                    items = new
                    {
                        type = "object",
                        required = new[] { "code", "value" },
                        properties = new
                        {
                            code = new
                            {
                                type = "string",
                                description = "Metric code identifier (e.g., 'weight', 'sleep_hours', 'steps')",
                                examples = new[] { "weight", "sleep_hours", "steps", "mood", "energy" }
                            },
                            value = new
                            {
                                type = "number",
                                description = "Numeric value of the metric"
                            },
                            metadata = new
                            {
                                type = "object",
                                description = "Optional additional data about the metric",
                                additionalProperties = true
                            }
                        }
                    }
                }
            },
            example = new
            {
                source = "n8n",
                timestamp = DateTime.UtcNow.ToString("O"),
                metrics = new object[]
                {
                    new { code = "weight", value = 75.5, metadata = new Dictionary<string, object> { { "unit", "kg" } } },
                    new { code = "sleep_hours", value = 7.5, metadata = (object?)null },
                    new { code = "steps", value = 10500, metadata = (object?)null }
                }
            }
        };

        return Ok(schema);
    }
}

// DTOs
public class CollectRequest
{
    public string? Source { get; set; }
    public DateTime? Timestamp { get; set; }
    public List<MetricInput> Metrics { get; set; } = new();
}

public class MetricInput
{
    public string Code { get; set; } = "";
    public decimal Value { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class CollectResponse
{
    public bool Success { get; set; }
    public CollectResponseData Data { get; set; } = new();
}

public class CollectResponseData
{
    public int Recorded { get; set; }
    public List<MetricError> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public Guid EventId { get; set; }
}

public class MetricError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}

public class ErrorResponse
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}

public class EventLogResponse
{
    public List<EventLogItem> Events { get; set; } = new();
    public int Total { get; set; }
}

public class EventLogItem
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = "";
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
}
