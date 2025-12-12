namespace LifeOS.Application.Interfaces;

public interface IMetricAggregationService
{
    Task<decimal?> AggregateMetricAsync(
        string metricCode,
        Guid userId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
