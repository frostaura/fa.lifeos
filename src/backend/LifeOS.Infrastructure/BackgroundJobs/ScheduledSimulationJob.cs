using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily job (4 AM) to run scenarios marked for scheduled execution
/// </summary>
public class ScheduledSimulationJob
{
    private readonly ILifeOSDbContext _context;
    private readonly ISimulationEngine _simulationEngine;
    private readonly ILogger<ScheduledSimulationJob> _logger;

    public ScheduledSimulationJob(
        ILifeOSDbContext context,
        ISimulationEngine simulationEngine,
        ILogger<ScheduledSimulationJob> logger)
    {
        _context = context;
        _simulationEngine = simulationEngine;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled simulation job");

        try
        {
            // Find all scenarios marked for scheduled execution (baseline scenarios)
            var scheduledScenarios = await _context.SimulationScenarios
                .Where(s => s.IsBaseline)
                .ToListAsync(cancellationToken);

            var successCount = 0;
            var failCount = 0;

            foreach (var scenario in scheduledScenarios)
            {
                try
                {
                    _logger.LogDebug("Running scheduled simulation for scenario {ScenarioId}: {Name}", 
                        scenario.Id, scenario.Name);

                    var result = await _simulationEngine.RunSimulationAsync(
                        scenario.UserId, scenario.Id, recalculateFromStart: true, cancellationToken);
                    
                    scenario.LastRunAt = DateTime.UtcNow;
                    scenario.UpdatedAt = DateTime.UtcNow;
                    
                    successCount++;
                    
                    _logger.LogDebug("Simulation completed for scenario {ScenarioId}", scenario.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to run simulation for scenario {ScenarioId}", scenario.Id);
                    failCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Scheduled simulation job completed. Success: {Success}, Failed: {Failed}",
                successCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled simulation job failed");
            throw;
        }
    }
}
