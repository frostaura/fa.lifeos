using LifeOS.Application.DTOs.Simulations;

namespace LifeOS.Application.Services;

public interface ISimulationEngine
{
    /// <summary>
    /// Run a simulation for a given scenario and generate projections
    /// </summary>
    Task<RunSimulationData> RunSimulationAsync(
        Guid userId, 
        Guid scenarioId, 
        bool recalculateFromStart = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get projections for a scenario
    /// </summary>
    Task<ProjectionData> GetProjectionsAsync(
        Guid userId,
        Guid scenarioId,
        DateOnly? from = null,
        DateOnly? to = null,
        string granularity = "monthly",
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate milestone dates (e.g., when will net worth reach X?)
    /// </summary>
    Task<List<MilestoneResult>> CalculateMilestonesAsync(
        Guid userId,
        Guid scenarioId,
        List<decimal> targetNetWorths,
        CancellationToken cancellationToken = default);
}
