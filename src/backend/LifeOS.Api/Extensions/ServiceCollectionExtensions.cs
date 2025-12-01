using System.Text;
using LifeOS.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

namespace LifeOS.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }

    public static IServiceCollection AddLifeOSCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = new CorsSettings();
        configuration.GetSection(CorsSettings.SectionName).Bind(corsSettings);
        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));

        services.AddCors(options =>
        {
            options.AddPolicy("LifeOSPolicy", policy =>
            {
                var origins = corsSettings.AllowedOrigins.Length > 0 
                    ? corsSettings.AllowedOrigins 
                    : new[] { "http://localhost:5173", "http://localhost:3000" };

                policy
                    .WithOrigins(origins)
                    .AllowCredentials()
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                    .WithHeaders(
                        "Content-Type",
                        "Authorization",
                        "X-Requested-With",
                        "X-Request-ID",
                        "X-API-Key"
                    )
                    .WithExposedHeaders(
                        "X-Request-ID",
                        "X-RateLimit-Limit",
                        "X-RateLimit-Remaining",
                        "X-RateLimit-Reset"
                    )
                    .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });

        return services;
    }

    public static IServiceCollection AddLifeOSRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new RateLimitSettings();
        configuration.GetSection(RateLimitSettings.SectionName).Bind(settings);
        services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? (int)retry.TotalSeconds
                    : 60;

                context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
                context.HttpContext.Response.Headers["X-RateLimit-Limit"] = settings.DefaultPermitLimit.ToString();
                context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.HttpContext.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfter).ToUnixTimeSeconds().ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = "RATE_LIMITED",
                        message = "Too many requests. Please try again later.",
                        retryAfter
                    }
                }, cancellationToken: token);
            };

            // Global default: 100 requests/minute
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.DefaultPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.DefaultWindowMinutes),
                        QueueLimit = 0
                    }));

            // Auth endpoints: 5 requests/15 minutes
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.AuthPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.AuthWindowMinutes),
                        QueueLimit = 0
                    }));

            // Metrics ingestion: 1000 requests/minute
            options.AddPolicy("metrics", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetApiKeyOrIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.MetricsPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.MetricsWindowMinutes),
                        QueueLimit = 10
                    }));
        });

        return services;
    }

    private static string GetApiKeyOrIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) && !string.IsNullOrEmpty(apiKey))
        {
            return apiKey.ToString();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static IServiceCollection AddLifeOSSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LifeOS API",
                Version = "v1",
                Description = "Personal Life Operating System API - Track dimensions, metrics, finances, and simulations",
                Contact = new OpenApiContact
                {
                    Name = "LifeOS Team"
                }
            });

            // Map DateOnly and TimeOnly types (including nullable)
            c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
            c.MapType<DateOnly?>(() => new OpenApiSchema { Type = "string", Format = "date", Nullable = true });
            c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
            c.MapType<TimeOnly?>(() => new OpenApiSchema { Type = "string", Format = "time", Nullable = true });

            // Add JWT Bearer authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            // Add API Key authentication
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key for automation endpoints. Format: lifeos_prefix_secret",
                Name = "X-API-Key",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Group endpoints by tags
            c.TagActionsBy(api =>
            {
                if (api.GroupName != null) return new[] { api.GroupName };
                if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
                {
                    return new[] { controllerActionDescriptor.ControllerName };
                }
                return new[] { "Other" };
            });
        });

        return services;
    }
}
