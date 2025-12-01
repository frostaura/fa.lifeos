using LifeOS.Infrastructure.Services.Authentication;
using Xunit;

namespace LifeOS.Tests.AuthenticationServices;

public class ApiKeyServiceTests
{
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyServiceTests()
    {
        _apiKeyService = new ApiKeyService();
    }

    [Fact]
    public void GenerateApiKey_ShouldReturnValidKey()
    {
        // Act
        var (fullKey, prefix, hash) = _apiKeyService.GenerateApiKey();

        // Assert
        Assert.NotNull(fullKey);
        Assert.NotNull(prefix);
        Assert.NotNull(hash);
        Assert.StartsWith("lifeos_", fullKey);
        Assert.StartsWith("lifeos_", prefix);
        Assert.Contains("_", fullKey);
    }

    [Fact]
    public void ValidateApiKey_WithCorrectKey_ShouldReturnTrue()
    {
        // Arrange
        var (fullKey, _, hash) = _apiKeyService.GenerateApiKey();

        // Act
        var result = _apiKeyService.ValidateApiKey(fullKey, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateApiKey_WithIncorrectKey_ShouldReturnFalse()
    {
        // Arrange
        var (_, _, hash) = _apiKeyService.GenerateApiKey();
        var wrongKey = "lifeos_wrongkey_wrongsecret";

        // Act
        var result = _apiKeyService.ValidateApiKey(wrongKey, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetPrefixFromKey_ShouldReturnPrefix()
    {
        // Arrange
        var key = "lifeos_abc12345_secretsecretsecretsecret";

        // Act
        var prefix = _apiKeyService.GetPrefixFromKey(key);

        // Assert
        Assert.Equal("lifeos_abc12345", prefix);
    }

    [Fact]
    public void GetPrefixFromKey_WithEmptyKey_ShouldReturnEmpty()
    {
        // Arrange
        var key = "";

        // Act
        var prefix = _apiKeyService.GetPrefixFromKey(key);

        // Assert
        Assert.Equal("", prefix);
    }

    [Fact]
    public void GenerateApiKey_TwoCalls_ShouldProduceDifferentKeys()
    {
        // Act
        var (key1, _, _) = _apiKeyService.GenerateApiKey();
        var (key2, _, _) = _apiKeyService.GenerateApiKey();

        // Assert
        Assert.NotEqual(key1, key2);
    }
}
