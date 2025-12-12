using LifeOS.Application.Common.Interfaces;
using LifeOS.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Api.Mcp;

/// <summary>
/// Result of API key validation.
/// </summary>
public record ApiKeyValidationResult(bool IsValid, Guid UserId, string? Error = null);

/// <summary>
/// Interface for validating MCP API keys and retrieving the associated user.
/// </summary>
public interface IMcpApiKeyValidator
{
    /// <summary>
    /// Validates an API key and returns the associated user ID.
    /// </summary>
    /// <param name="apiKey">The API key to validate (format: lifeos_prefix_secret)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with user ID if valid</returns>
    Task<ApiKeyValidationResult> ValidateAndGetUserIdAsync(string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IMcpApiKeyValidator that validates API keys against the database.
/// </summary>
public class McpApiKeyValidator : IMcpApiKeyValidator
{
    private readonly ILifeOSDbContext _dbContext;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<McpApiKeyValidator> _logger;

    public McpApiKeyValidator(
        ILifeOSDbContext dbContext,
        IApiKeyService apiKeyService,
        ILogger<McpApiKeyValidator> logger)
    {
        _dbContext = dbContext;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    public async Task<ApiKeyValidationResult> ValidateAndGetUserIdAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ApiKeyValidationResult(false, Guid.Empty, "API key is required");
        }

        // Validate API key format
        if (!apiKey.StartsWith("lifeos_"))
        {
            return new ApiKeyValidationResult(false, Guid.Empty, "Invalid API key format. Must start with 'lifeos_'");
        }

        // Extract prefix and look up key
        var prefix = _apiKeyService.GetPrefixFromKey(apiKey);
        if (string.IsNullOrEmpty(prefix))
        {
            return new ApiKeyValidationResult(false, Guid.Empty, "Invalid API key format");
        }

        var storedKey = await _dbContext.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyPrefix == prefix && !k.IsRevoked, cancellationToken);

        if (storedKey == null)
        {
            _logger.LogWarning("API key not found or revoked for prefix: {Prefix}", prefix);
            return new ApiKeyValidationResult(false, Guid.Empty, "API key not found or revoked");
        }

        // Check expiration
        if (storedKey.ExpiresAt.HasValue && storedKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("API key expired for prefix: {Prefix}", prefix);
            return new ApiKeyValidationResult(false, Guid.Empty, "API key has expired");
        }

        // Validate the full key against stored hash
        if (!_apiKeyService.ValidateApiKey(apiKey, storedKey.KeyHash))
        {
            _logger.LogWarning("API key validation failed for prefix: {Prefix}", prefix);
            return new ApiKeyValidationResult(false, Guid.Empty, "Invalid API key");
        }

        // Update last used timestamp
        storedKey.LastUsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("API key validated successfully for user: {UserId}", storedKey.UserId);
        return new ApiKeyValidationResult(true, storedKey.UserId);
    }
}
