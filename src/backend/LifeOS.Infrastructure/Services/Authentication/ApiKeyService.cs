using System.Security.Cryptography;
using System.Text;

namespace LifeOS.Infrastructure.Services.Authentication;

public interface IApiKeyService
{
    (string fullKey, string prefix, string hash) GenerateApiKey();
    bool ValidateApiKey(string providedKey, string storedHash);
    string GetPrefixFromKey(string key);
}

public class ApiKeyService : IApiKeyService
{
    private const string KeyPrefix = "lifeos_";
    private const int PrefixLength = 8;
    private const int SecretLength = 32;

    public (string fullKey, string prefix, string hash) GenerateApiKey()
    {
        var prefix = GenerateRandomString(PrefixLength);
        var secret = GenerateRandomString(SecretLength);
        var fullKey = $"{KeyPrefix}{prefix}_{secret}";
        var hash = HashKey(fullKey);

        return (fullKey, $"{KeyPrefix}{prefix}", hash);
    }

    public bool ValidateApiKey(string providedKey, string storedHash)
    {
        var providedHash = HashKey(providedKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedHash),
            Encoding.UTF8.GetBytes(storedHash)
        );
    }

    public string GetPrefixFromKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        
        var parts = key.Split('_');
        if (parts.Length >= 2)
        {
            return $"{parts[0]}_{parts[1]}";
        }
        
        return string.Empty;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static string HashKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
