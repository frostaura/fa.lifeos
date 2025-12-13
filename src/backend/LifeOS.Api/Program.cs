using Fido2NetLib;
using Hangfire;
using Hangfire.Dashboard;
using LifeOS.Api.Extensions;
using LifeOS.Api.Mcp;
using LifeOS.Api.Middleware;
using LifeOS.Application;
using LifeOS.Infrastructure;
using LifeOS.Infrastructure.BackgroundJobs;
using LifeOS.Infrastructure.Configuration;
using LifeOS.Infrastructure.Hubs;
using LifeOS.Infrastructure.Services.Seeding;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.AspNetCore;

// Application metadata constants
const string AppName = "LifeOS";
const string AppVersion = "1.0.0";
const string AppDescription = "Personal Life Operating System API - Track dimensions, metrics, finances, and simulations";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();

// Add Session support for FIDO2
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add FIDO2 for passkey/biometric authentication
builder.Services.AddFido2(options =>
{
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? "localhost";
    options.ServerName = "LifeOS";
    options.Origins = new HashSet<string>
    {
        builder.Configuration["Fido2:Origin"] ?? "http://localhost:5173",
        "http://localhost:5001",
        "http://localhost:5000"
    };
    options.TimestampDriftTolerance = 300000; // 5 minutes
});

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Add SignalR
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddLifeOSCors(builder.Configuration);

// Add Rate Limiting
builder.Services.AddLifeOSRateLimiting(builder.Configuration);

// Add Swagger with JWT support
builder.Services.AddLifeOSSwagger(AppName, AppVersion, AppDescription);

// Add Application layer services (MediatR, AutoMapper, FluentValidation)
builder.Services.AddApplicationServices();

// Add Infrastructure layer services (EF Core, DbContext, Auth services, Hangfire)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add AutoMapper from Api assembly
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Add MediatR from Api assembly
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Add MCP Server with HTTP transport and tools from this assembly
builder.Services.AddScoped<IMcpApiKeyValidator, McpApiKeyValidator>();
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = $"{AppName} MCP",
            Version = AppVersion,
            Description = @"
            LifeOS MCP Server - A comprehensive personal life management system accessible via Model Context Protocol.

            CORE CONCEPTS:
            • Dimensions: Life areas (Health, Career, Finance, Mind, Relationships, etc.) with weighted scores contributing to overall LifeOS score
            • Metrics: Quantifiable data points (sleep_hours, weight, mood) linked to dimensions, supporting number/boolean/enum value types with targets and aggregation
            • Tasks: Habits, todos, and recurring tasks with frequency tracking (daily/weekly/monthly), streak management, and dimension linking
            • Milestones: Long-term goals with progress tracking, target dates, and sub-task decomposition
            • Primary Stats: RPG-style attributes (Strength, Wisdom, Charisma, Composure, Energy, Influence, Vitality) with current levels and targets (0-100 scale)
            • Identity Profile: User archetype, core values, and stat targets defining personal development direction
            • Accounts: Financial accounts (Bank, Investment, Loan, Credit, Crypto, Property) with multi-currency support
            • Transactions: Income/expense tracking with categories, automatic balance updates, and spending analysis

            MCP TOOLS AVAILABLE:
            Dashboard: getDashboardSnapshot - Unified view of LifeOS score, primary stats, today's tasks, net worth, upcoming events
            Dimensions: listDimensions, getDimension, createDimension, updateDimensionWeight, deleteDimension
            Metrics: listMetrics, recordMetrics (batch recording), getMetricHistory (with aggregation: raw/daily/weekly/monthly)
            Tasks: listTasks, getTask, createTask, updateTask, deleteTask, completeTask (updates streaks)
            Milestones: listMilestones, getMilestone, createMilestone, updateMilestone, deleteMilestone, completeMilestone
            Identity: getIdentityProfile, updateIdentityTargets
            Accounts: listAccounts, getAccount, createAccount, updateAccount, deleteAccount, updateAccountBalance
            Transactions: listTransactions, getTransaction, createTransaction, updateTransaction, deleteTransaction, getTransactionCategories
            Reviews: getWeeklyReview (streaks, health index changes), getMonthlyReview (score trends, net worth, milestone progress)

            AUTHENTICATION: All tools require an API key parameter for user authentication.

            USE CASES: AI assistants can help users track habits, log health metrics, manage finances, set and monitor goals, conduct life reviews, and optimize personal development through data-driven insights."
        };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline
// Exception handling middleware should be first
app.UseExceptionHandling();

// Security headers
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{AppName} API {AppVersion}");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = $"{AppName} API";
    });
}

app.UseHttpsRedirection();
app.UseCors("LifeOSPolicy");
app.UseRateLimiter();

// Session middleware (required for FIDO2)
app.UseSession();

// API Key authentication middleware (before JWT auth)
app.UseApiKeyAuthentication();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (with basic auth in production)
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(hangfireConnectionString))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = app.Environment.IsDevelopment()
            ? Array.Empty<IDashboardAuthorizationFilter>()
            : new[] { new HangfireBasicAuthFilter() },
        DashboardTitle = "LifeOS Background Jobs"
    });

    // Configure recurring jobs
    ConfigureRecurringJobs();
}

app.MapControllers();

// Map MCP endpoint (anonymous access - authentication via apiKey parameter)
// Accessible at /mcp for MCP protocol clients
app.MapMcp("/mcp").AllowAnonymous();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notifications");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

// Apply migrations on startup (safe for all environments)
await MigrateDatabaseAsync(app);

// Seed data in development only
if (app.Environment.IsDevelopment())
{
    await SeedAdminUserAsync(app);
    await SeedDataAsync(app);
}

app.Run();

// Apply database migrations safely with retry logic
async Task MigrateDatabaseAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var maxRetries = 5;
    var delay = TimeSpan.FromSeconds(5);

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LifeOS.Infrastructure.Persistence.LifeOSDbContext>();

            logger.LogInformation("Attempting database migration (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);

            // EnsureCreated is skipped when migrations exist - MigrateAsync handles creation
            await context.Database.MigrateAsync();

            logger.LogInformation("Database migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Database migration attempt {Attempt} failed. Retrying in {Delay} seconds...", attempt, delay.TotalSeconds);
            await Task.Delay(delay);
            delay *= 2; // Exponential backoff
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed after {MaxRetries} attempts.", maxRetries);
            throw;
        }
    }
}

void ConfigureRecurringJobs()
{
    // FX Rate Refresh - Every hour
    RecurringJob.AddOrUpdate<FxRateRefreshJob>(
        "fx-rate-refresh",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Hourly);

    // Score Recomputation - Daily at 3 AM
    RecurringJob.AddOrUpdate<ScoreRecomputationJob>(
        "score-recomputation",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily(3));

    // Streak Evaluation - Daily at midnight
    RecurringJob.AddOrUpdate<StreakEvaluationJob>(
        "streak-evaluation",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily(0));

    // Task Auto-Evaluation - Every hour
    RecurringJob.AddOrUpdate<TaskEvaluationBackgroundJob>(
        "task-evaluation",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Hourly);

    // Scheduled Simulations - Daily at 4 AM
    RecurringJob.AddOrUpdate<ScheduledSimulationJob>(
        "scheduled-simulations",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily(4));

    // Net Worth Snapshot - Daily at 11:59 PM
    RecurringJob.AddOrUpdate<NetWorthSnapshotJob>(
        "net-worth-snapshot",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily(23, 59));
}

// Seed admin user for development (biometric-only, no password)
async Task SeedAdminUserAsync(WebApplication app)
{
    // Biometric-only login - no password users seeded
    // Users must register with passkey
    Console.WriteLine("Biometric-only authentication enabled. Register with passkey at /api/auth/passkey/register/begin");
}

// Seed dimensions and other data
async Task SeedDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync();
}

// Make Program class accessible for tests
public partial class Program { }
