using System.ComponentModel.DataAnnotations;

namespace LifeOS.Api.Contracts.Auth;

public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;
}

public record DevLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Required]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public LoginData Data { get; init; } = new();
}

public record LoginData
{
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = "Bearer";
}

public record RefreshResponse
{
    public LoginData Data { get; init; } = new();
}

public record CreateApiKeyRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;
    
    public string[] Scopes { get; init; } = Array.Empty<string>();
    
    public DateTime? ExpiresAt { get; init; }
}

public record CreateApiKeyResponse
{
    public ApiKeyData Data { get; init; } = new();
}

public record ApiKeyData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;  // Only returned once on creation
    public string Prefix { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record ApiKeyListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Prefix { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
}
