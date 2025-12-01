using LifeOS.Api.Extensions;
using LifeOS.Api.Middleware;
using LifeOS.Application;
using LifeOS.Infrastructure;
using LifeOS.Infrastructure.BackgroundJobs;
using LifeOS.Infrastructure.Configuration;
using LifeOS.Infrastructure.Services.Seeding;
using Fido2NetLib;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
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

// Add CORS
builder.Services.AddLifeOSCors(builder.Configuration);

// Add Rate Limiting
builder.Services.AddLifeOSRateLimiting(builder.Configuration);

// Add Swagger with JWT support
builder.Services.AddLifeOSSwagger();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LifeOS API v1");
        c.RoutePrefix = "swagger";
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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

// Apply migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    await MigrateDatabaseAsync(app);
    await SeedAdminUserAsync(app);
    await SeedDataAsync(app);
}

app.Run();

// Apply database migrations
async Task MigrateDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LifeOS.Infrastructure.Persistence.LifeOSDbContext>();
    await context.Database.MigrateAsync();
    Console.WriteLine("Database migrations applied.");
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
