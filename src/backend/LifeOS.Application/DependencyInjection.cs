using System.Reflection;
using FluentValidation;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Interfaces.Mcp;
using LifeOS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LifeOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Application services
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<IScoreCalculator, ScoreCalculator>();
        services.AddScoped<ILongevityEstimator, LongevityEstimator>();
        services.AddScoped<ILongevityCalculator, LongevityCalculator>();
        services.AddScoped<IWealthHealthScoreService, WealthHealthScoreService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<IMetricIngestionService, MetricIngestionService>();
        services.AddScoped<IMetricAggregationService, MetricAggregationService>();
        services.AddScoped<ITaskEvaluationService, TaskEvaluationService>();
        services.AddScoped<IHealthIndexCalculator, HealthIndexCalculator>();
        services.AddScoped<IBehavioralAdherenceCalculator, BehavioralAdherenceCalculator>();
        services.AddScoped<IWealthHealthCalculator, WealthHealthCalculator>();
        services.AddScoped<ILifeOSScoreAggregator, LifeOSScoreAggregator>();
        services.AddScoped<IPrimaryStatsCalculator, PrimaryStatsCalculator>();
        services.AddSingleton<IConditionParser, ConditionParser>();
        services.AddScoped<ISimulationEngine, SimulationEngine>();

        // MCP Tool Handlers - Auto-register all implementations of IMcpToolHandler
        var mcpHandlerType = typeof(IMcpToolHandler);
        var mcpHandlers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && mcpHandlerType.IsAssignableFrom(t));

        foreach (var handlerType in mcpHandlers)
        {
            services.AddScoped(mcpHandlerType, handlerType);
        }

        return services;
    }
}
