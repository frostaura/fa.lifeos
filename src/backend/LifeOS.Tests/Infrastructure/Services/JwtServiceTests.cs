using LifeOS.Infrastructure.Configuration;
using LifeOS.Infrastructure.Services.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace LifeOS.Tests.AuthenticationServices;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly JwtSettings _settings;

    public JwtServiceTests()
    {
        _settings = new JwtSettings
        {
            SecretKey = "LifeOS-Test-Secret-Key-Must-Be-At-Least-32-Characters-Long!",
            Issuer = "lifeos-api",
            Audience = "lifeos-client",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };
        
        _jwtService = new JwtService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = _jwtService.GenerateAccessToken(userId, email);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT format
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var token = _jwtService.GenerateRefreshToken(userId);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT format
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var token = _jwtService.GenerateAccessToken(userId, email);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var token = _jwtService.GenerateAccessToken(userId, email);

        // Act
        var extractedUserId = _jwtService.GetUserIdFromToken(token);

        // Assert
        Assert.NotNull(extractedUserId);
        Assert.Equal(userId, extractedUserId.Value);
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var extractedUserId = _jwtService.GetUserIdFromToken(invalidToken);

        // Assert
        Assert.Null(extractedUserId);
    }
}
