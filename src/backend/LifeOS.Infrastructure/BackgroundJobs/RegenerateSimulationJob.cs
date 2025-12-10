using LifeOS.Application.Commands.Simulations;
using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job to regenerate simulation in a separate DI scope
/// Prevents DbContext concurrency issues
/// </summary>
public class RegenerateSimulationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegenerateSimulationJob> _logger;

    public RegenerateSimulationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RegenerateSimulationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid userId, Guid scenarioId)
    {
        _logger.LogInformation("Starting simulation regeneration for user {UserId}, scenario {ScenarioId}", userId, scenarioId);

        try
        {
            // Create a NEW DI scope to avoid DbContext concurrency issues
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await mediator.Send(new RunSimulationCommand(
                userId,
                scenarioId,
                RecalculateFromStart: true
            ), CancellationToken.None);

            _logger.LogInformation("Simulation regeneration completed for scenario {ScenarioId}", scenarioId);

            // Notify user that projections have been updated
            await notificationService.NotifyProjectionsUpdatedAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate simulation for scenario {ScenarioId}", scenarioId);
            throw;
        }
    }
}
