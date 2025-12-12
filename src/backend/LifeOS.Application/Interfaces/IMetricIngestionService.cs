using LifeOS.Application.DTOs.Metrics;

namespace LifeOS.Application.Interfaces;

public interface IMetricIngestionService
{
    Task<MetricRecordResponse> ProcessNestedMetrics(MetricRecordRequest request, Guid userId, CancellationToken cancellationToken = default);
}
