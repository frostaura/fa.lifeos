using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Infrastructure.BackgroundJobs;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Services.Authentication;
using LifeOS.Infrastructure.Services.FxRates;
using LifeOS.Infrastructure.Services.Seeding;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LifeOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<LifeOSDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(LifeOSDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                });
            }
            else
            {
                // Use in-memory database for development/testing when no connection string
                options.UseInMemoryDatabase("LifeOS");
            }
        });

        services.AddScoped<ILifeOSDbContext>(provider => 
            provider.GetRequiredService<LifeOSDbContext>());

        // Authentication services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IPasswordService, PasswordService>();

        // Data seeding
        services.AddScoped<IDataSeeder, DataSeeder>();

        // FX Rate Provider
        services.AddHttpClient<IFxRateProvider, CoinGeckoFxRateProvider>();
        services.AddMemoryCache();

        // Background Jobs
        services.AddScoped<FxRateRefreshJob>();
        services.AddScoped<ScoreRecomputationJob>();
        services.AddScoped<StreakEvaluationJob>();
        services.AddScoped<ScheduledSimulationJob>();
        services.AddScoped<NetWorthSnapshotJob>();

        // Hangfire Configuration
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(options => 
                        options.UseNpgsqlConnection(connectionString));
            });

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 2;
            });
        }

        return services;
    }
}
