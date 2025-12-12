using System.Security.Claims;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    private static readonly HashSet<string> ApiKeyEnabledPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/webhooks",
        "/api/metrics",
        "/api/collect",
        "/api/dimensions",
        "/api/accounts",
        "/api/health-data",
        "/api/tasks",
        "/api/tax-profiles",
        "/api/income-sources",
        "/api/expense-definitions",
        "/api/simulations",
        "/api/fx-rates",
        "/api/transactions",
        "/api/settings",
        "/api/mcp"
    };

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService, ILifeOSDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? "";

        // Only check API key for specific paths
        if (!IsApiKeyPath(path))
        {
            await _next(context);
            return;
        }

        // Check if already authenticated via JWT
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Check for X-API-Key header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            await _next(context);
            return;
        }

        var apiKeyValue = apiKeyHeader.ToString();
        if (string.IsNullOrEmpty(apiKeyValue))
        {
            await _next(context);
            return;
        }

        // Validate API key format and extract prefix
        var prefix = apiKeyService.GetPrefixFromKey(apiKeyValue);
        if (string.IsNullOrEmpty(prefix) || !apiKeyValue.StartsWith("lifeos_"))
        {
            _logger.LogWarning("Invalid API key format for path: {Path}", path);
            await _next(context);
            return;
        }

        // Look up API key in database by prefix
        var storedKey = await dbContext.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyPrefix == prefix && !k.IsRevoked);

        if (storedKey == null)
        {
            _logger.LogWarning("API key not found or revoked for prefix: {Prefix}", prefix);
            await _next(context);
            return;
        }

        // Validate the full key against stored hash
        if (!apiKeyService.ValidateApiKey(apiKeyValue, storedKey.KeyHash))
        {
            _logger.LogWarning("API key validation failed for prefix: {Prefix}", prefix);
            await _next(context);
            return;
        }

        // Update last used timestamp
        storedKey.LastUsedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Create claims identity for the user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, storedKey.UserId.ToString()),
            new(ClaimTypes.Email, storedKey.User.Email),
            new(ClaimTypes.Name, storedKey.User.Username ?? storedKey.User.Email),
            new("auth_type", "api_key"),
            new("api_key_prefix", prefix)
        };

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        // Mark as API key authenticated
        context.Items["ApiKeyPrefix"] = prefix;
        context.Items["IsApiKeyAuthenticated"] = true;
        _logger.LogInformation("API key authentication successful for user: {UserId}, path: {Path}", storedKey.UserId, path);

        await _next(context);
    }

    private static bool IsApiKeyPath(string path)
    {
        return ApiKeyEnabledPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
