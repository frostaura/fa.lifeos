using LifeOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly LifeOSDbContext _context;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(LifeOSDbContext context, ILogger<SettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Get current user profile settings
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User not found" } });

        var dimensions = await _context.Dimensions.OrderBy(d => d.SortOrder).ToListAsync();
        
        // Get or create user settings
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (userSettings == null)
        {
            userSettings = new Domain.Entities.UserSettings { UserId = userId };
            _context.UserSettings.Add(userSettings);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            data = new
            {
                email = user.Email,
                username = user.Username,
                homeCurrency = user.HomeCurrency,
                dateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
                lifeExpectancyBaseline = user.LifeExpectancyBaseline,
                defaultAssumptions = JsonSerializer.Deserialize<object>(user.DefaultAssumptions),
                dimensions = dimensions.Select(d => new
                {
                    id = d.Id,
                    code = d.Code,
                    name = d.Name,
                    weight = d.DefaultWeight,
                    icon = d.Icon
                }),
                appearance = new
                {
                    orbColor1 = userSettings.OrbColor1,
                    orbColor2 = userSettings.OrbColor2,
                    orbColor3 = userSettings.OrbColor3,
                    accentColor = userSettings.AccentColor,
                    baseFontSize = userSettings.BaseFontSize,
                    themeMode = userSettings.ThemeMode
                }
            }
        });
    }

    /// <summary>
    /// Update user profile settings
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User not found" } });

        if (!string.IsNullOrEmpty(request.Username))
            user.Username = request.Username;

        if (!string.IsNullOrEmpty(request.HomeCurrency))
            user.HomeCurrency = request.HomeCurrency;

        if (request.DateOfBirth.HasValue)
            user.DateOfBirth = request.DateOfBirth;

        if (request.LifeExpectancyBaseline.HasValue)
            user.LifeExpectancyBaseline = request.LifeExpectancyBaseline.Value;

        if (request.DefaultAssumptions != null)
            user.DefaultAssumptions = JsonSerializer.Serialize(request.DefaultAssumptions);

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} updated profile settings", userId);

        return Ok(new { data = new { message = "Profile updated successfully" } });
    }

    /// <summary>
    /// Update dimension weights (must total 100%)
    /// </summary>
    [HttpPut("dimensions/weights")]
    public async Task<IActionResult> UpdateDimensionWeights([FromBody] UpdateDimensionWeightsRequest request)
    {
        if (request.Weights == null || !request.Weights.Any())
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "Weights are required" } });

        var totalWeight = request.Weights.Sum(w => w.Weight);
        if (Math.Abs(totalWeight - 100) > 0.01m)
            return BadRequest(new { error = new { code = "INVALID_WEIGHTS", message = $"Weights must total 100%, got {totalWeight}%" } });

        var dimensions = await _context.Dimensions.ToListAsync();
        
        foreach (var weightUpdate in request.Weights)
        {
            var dimension = dimensions.FirstOrDefault(d => d.Id == weightUpdate.DimensionId);
            if (dimension != null)
            {
                dimension.DefaultWeight = weightUpdate.Weight / 100m;  // Convert percentage to decimal
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated dimension weights");

        return Ok(new { data = new { message = "Dimension weights updated successfully" } });
    }
    
    /// <summary>
    /// Update appearance settings (orb colors)
    /// </summary>
    [HttpPut("appearance")]
    public async Task<IActionResult> UpdateAppearance([FromBody] UpdateAppearanceRequest request)
    {
        var userId = GetUserId();
        var userSettings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (userSettings == null)
        {
            userSettings = new Domain.Entities.UserSettings { UserId = userId };
            _context.UserSettings.Add(userSettings);
        }

        if (!string.IsNullOrEmpty(request.OrbColor1))
            userSettings.OrbColor1 = request.OrbColor1;
            
        if (!string.IsNullOrEmpty(request.OrbColor2))
            userSettings.OrbColor2 = request.OrbColor2;
            
        if (!string.IsNullOrEmpty(request.OrbColor3))
            userSettings.OrbColor3 = request.OrbColor3;
            
        if (!string.IsNullOrEmpty(request.AccentColor))
            userSettings.AccentColor = request.AccentColor;
            
        if (request.BaseFontSize.HasValue)
            userSettings.BaseFontSize = request.BaseFontSize.Value;
            
        if (!string.IsNullOrEmpty(request.ThemeMode))
            userSettings.ThemeMode = request.ThemeMode;

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} updated appearance settings", userId);

        return Ok(new { data = new { message = "Appearance settings updated successfully" } });
    }
}

public record UpdateProfileRequest
{
    public string? Username { get; init; }
    public string? HomeCurrency { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public decimal? LifeExpectancyBaseline { get; init; }
    public DefaultAssumptions? DefaultAssumptions { get; init; }
}

public record DefaultAssumptions
{
    public decimal? InflationRateAnnual { get; init; }
    public decimal? DefaultGrowthRate { get; init; }
    public int? RetirementAge { get; init; }
}

public record UpdateDimensionWeightsRequest
{
    public List<DimensionWeight>? Weights { get; init; }
}

public record DimensionWeight
{
    public Guid DimensionId { get; init; }
    public decimal Weight { get; init; }
}

public record UpdateAppearanceRequest
{
    public string? OrbColor1 { get; init; }
    public string? OrbColor2 { get; init; }
    public string? OrbColor3 { get; init; }
    public string? AccentColor { get; init; }
    public decimal? BaseFontSize { get; init; }
    public string? ThemeMode { get; init; }
}
