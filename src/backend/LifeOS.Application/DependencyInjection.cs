using System.Reflection;
using FluentValidation;
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
        services.AddScoped<IWealthHealthScoreService, WealthHealthScoreService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddSingleton<IConditionParser, ConditionParser>();
        services.AddScoped<ISimulationEngine, SimulationEngine>();

        return services;
    }
}
