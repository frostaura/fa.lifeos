using LifeOS.Infrastructure.Services.Authentication;
using Xunit;

namespace LifeOS.Tests.AuthenticationServices;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void Hash_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.Hash(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
        Assert.Contains(".", hash); // Format: salt.hash
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordService.Hash(password);

        // Act
        var result = _passwordService.Verify(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _passwordService.Hash(password);

        // Act
        var result = _passwordService.Verify(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Hash_SameTwice_ShouldProduceDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.Hash(password);
        var hash2 = _passwordService.Hash(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // Different salts
    }

    [Fact]
    public void Verify_WithInvalidHashFormat_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid_hash_without_dot";

        // Act
        var result = _passwordService.Verify(password, invalidHash);

        // Assert
        Assert.False(result);
    }
}
