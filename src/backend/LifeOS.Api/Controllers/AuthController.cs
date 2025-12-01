using LifeOS.Api.Configuration;
using LifeOS.Api.Contracts.Auth;
using LifeOS.Infrastructure.Persistence;
using LifeOS.Infrastructure.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly LifeOSDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        LifeOSDbContext context,
        IJwtService jwtService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Login is now biometric-only. Use /api/auth/passkey/login/begin instead.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return BadRequest(new { 
            error = new { 
                code = "BIOMETRIC_ONLY", 
                message = "Password login is disabled. Please use biometric authentication.",
                redirect = "/api/auth/passkey/login/begin"
            } 
        });
    }

    /// <summary>
    /// Refresh access token using refresh token cookie
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("lifeos_refresh_token", out var refreshToken))
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Refresh token required" } });
        }

        var userId = _jwtService.GetUserIdFromToken(refreshToken);
        if (userId == null)
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Invalid refresh token" } });
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "User not found" } });
        }

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);

        return Ok(new RefreshResponse
        {
            Data = new LoginData
            {
                AccessToken = accessToken,
                ExpiresIn = _jwtSettings.AccessTokenMinutes * 60,
                TokenType = "Bearer"
            }
        });
    }

    /// <summary>
    /// Invalidate current session and clear refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        // Clear refresh token cookie
        Response.Cookies.Delete("lifeos_refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });

        _logger.LogInformation("User logged out");
        return NoContent();
    }

    /// <summary>
    /// Development-only endpoint for creating test sessions without biometrics.
    /// Only available in Development environment.
    /// </summary>
    [HttpPost("dev-login")]
    [AllowAnonymous]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request, [FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            return StatusCode(403, new { error = new { code = "FORBIDDEN", message = "This endpoint is only available in development mode" } });
        }

        // Find or create user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            // Check if username already exists and make unique if needed
            var baseUsername = request.DisplayName ?? "Dev User";
            var username = baseUsername;
            var counter = 1;
            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}_{counter++}";
            }
            
            user = new Domain.Entities.User
            {
                Email = request.Email,
                Username = username,
                HomeCurrency = "ZAR",
                PasswordHash = "DEV_LOGIN_NO_PASSWORD" // Placeholder for dev login
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created dev user: {Email}", request.Email);
        }

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

        // Set refresh token cookie
        Response.Cookies.Append("lifeos_refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Allow HTTP in development
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });

        _logger.LogInformation("Dev login successful for: {Email}", request.Email);

        return Ok(new RefreshResponse
        {
            Data = new LoginData
            {
                AccessToken = accessToken,
                ExpiresIn = _jwtSettings.AccessTokenMinutes * 60,
                TokenType = "Bearer"
            }
        });
    }
}

public class DevLoginRequest
{
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
}
