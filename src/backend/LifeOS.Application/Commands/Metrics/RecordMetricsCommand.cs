using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Metrics;
using LifeOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Metrics;

public record RecordMetricsCommand(
    Guid UserId,
    DateTime? Timestamp,
    string Source,
    Dictionary<string, decimal?> Metrics
) : IRequest<RecordMetricsResponse>;

public class RecordMetricsCommandHandler : IRequestHandler<RecordMetricsCommand, RecordMetricsResponse>
{
    private readonly ILifeOSDbContext _context;

    public RecordMetricsCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<RecordMetricsResponse> Handle(RecordMetricsCommand request, CancellationToken cancellationToken)
    {
        var timestamp = request.Timestamp ?? DateTime.UtcNow;
        var results = new List<MetricRecordResult>();
        var recorded = 0;
        var failed = 0;

        // Create event log entry for tracking
        var eventLog = new ApiEventLog
        {
            UserId = request.UserId,
            EventType = "metric_record",
            Source = request.Source ?? "playground",
            RequestPayload = JsonSerializer.Serialize(new { timestamp, source = request.Source, metrics = request.Metrics }),
            Timestamp = DateTime.UtcNow,
            Status = "processing"
        };

        try
        {
            // Get all valid metric definitions
            var validCodes = await _context.MetricDefinitions
                .Where(m => m.IsActive)
                .Select(m => new { m.Code, m.MinValue, m.MaxValue })
                .ToDictionaryAsync(m => m.Code, cancellationToken);

            foreach (var (code, value) in request.Metrics)
            {
                // Skip null values
                if (!value.HasValue)
                {
                    continue;
                }

                // Validate metric code exists
                if (!validCodes.TryGetValue(code, out var metricDef))
                {
                    results.Add(new MetricRecordResult
                    {
                        Code = code,
                        Status = "failed",
                        Error = "Unknown metric code"
                    });
                    failed++;
                    continue;
                }

                // Validate value bounds
                if (metricDef.MinValue.HasValue && value.Value < metricDef.MinValue.Value)
                {
                    results.Add(new MetricRecordResult
                    {
                        Code = code,
                        Status = "failed",
                        Error = $"Value {value.Value} is below minimum {metricDef.MinValue.Value}"
                    });
                    failed++;
                    continue;
                }

                if (metricDef.MaxValue.HasValue && value.Value > metricDef.MaxValue.Value)
                {
                    results.Add(new MetricRecordResult
                    {
                        Code = code,
                        Status = "failed",
                        Error = $"Value {value.Value} is above maximum {metricDef.MaxValue.Value}"
                    });
                    failed++;
                    continue;
                }

                // Create metric record
                var record = new MetricRecord
                {
                    UserId = request.UserId,
                    MetricCode = code,
                    ValueNumber = value.Value,
                    RecordedAt = timestamp,
                    Source = request.Source
                };

                _context.MetricRecords.Add(record);
                
                results.Add(new MetricRecordResult
                {
                    Code = code,
                    Status = "created",
                    Id = record.Id
                });
                recorded++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Update event log with success
            eventLog.Status = recorded > 0 ? "success" : (failed > 0 ? "partial" : "empty");
            eventLog.ResponsePayload = JsonSerializer.Serialize(new { recorded, failed, results = results.Take(5) });
            _context.ApiEventLogs.Add(eventLog);
            await _context.SaveChangesAsync(cancellationToken);

            return new RecordMetricsResponse
            {
                Data = new RecordMetricsData
                {
                    Attributes = new RecordMetricsAttributes
                    {
                        Recorded = recorded,
                        Failed = failed,
                        Timestamp = timestamp,
                        Source = request.Source
                    },
                    Records = results
                }
            };
        }
        catch (Exception ex)
        {
            // Log error event
            eventLog.Status = "error";
            eventLog.ErrorMessage = ex.Message;
            _context.ApiEventLogs.Add(eventLog);
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
