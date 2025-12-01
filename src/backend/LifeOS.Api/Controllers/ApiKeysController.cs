using LifeOS.Domain.Entities;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/settings/api-keys")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly LifeOSDbContext _context;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        LifeOSDbContext context, 
        IApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger)
    {
        _context = context;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// List all API keys for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApiKeys()
    {
        var userId = GetUserId();
        var keys = await _context.ApiKeys
            .Where(k => k.UserId == userId && !k.IsRevoked)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new
            {
                id = k.Id,
                name = k.Name,
                keyPrefix = k.KeyPrefix,
                scopes = k.Scopes,
                createdAt = k.CreatedAt,
                expiresAt = k.ExpiresAt,
                lastUsedAt = k.LastUsedAt
            })
            .ToListAsync();

        return Ok(new { data = keys });
    }

    /// <summary>
    /// Create a new API key
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var userId = GetUserId();

        // Generate new key
        var (fullKey, prefix, hash) = _apiKeyService.GenerateApiKey();

        var apiKey = new ApiKey
        {
            UserId = userId,
            Name = request.Name ?? "API Key",
            KeyPrefix = prefix,
            KeyHash = hash,
            Scopes = request.Scopes ?? "metrics:write",
            ExpiresAt = request.ExpiresInDays.HasValue 
                ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value) 
                : null
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created API key {KeyPrefix} for user {UserId}", prefix, userId);

        // Return the full key ONLY on creation - it cannot be retrieved again
        return Ok(new
        {
            data = new
            {
                id = apiKey.Id,
                name = apiKey.Name,
                key = fullKey,  // Only returned on creation!
                keyPrefix = prefix,
                scopes = apiKey.Scopes,
                expiresAt = apiKey.ExpiresAt,
                warning = "Save this key now. It cannot be retrieved again."
            }
        });
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeApiKey(Guid id)
    {
        var userId = GetUserId();
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);

        if (apiKey == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "API key not found" } });

        apiKey.IsRevoked = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked API key {KeyId} for user {UserId}", id, userId);

        return Ok(new { data = new { message = "API key revoked successfully" } });
    }
}

public record CreateApiKeyRequest
{
    public string? Name { get; init; }
    public string? Scopes { get; init; }
    public int? ExpiresInDays { get; init; }
}
