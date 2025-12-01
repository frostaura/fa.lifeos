using LifeOS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LongevityController : ControllerBase
{
    private readonly ILongevityEstimator _longevityEstimator;
    private readonly ILogger<LongevityController> _logger;

    public LongevityController(
        ILongevityEstimator longevityEstimator,
        ILogger<LongevityController> logger)
    {
        _longevityEstimator = longevityEstimator;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        return userId;
    }

    /// <summary>
    /// Get current longevity estimate for the authenticated user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LongevityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEstimate(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        
        try
        {
            var estimate = await _longevityEstimator.CalculateEstimateAsync(userId, cancellationToken);

            var response = new LongevityResponse
            {
                Data = new LongevityData
                {
                    Type = "longevity",
                    Attributes = new LongevityAttributes
                    {
                        BaselineLifeExpectancy = estimate.BaselineLifeExpectancy,
                        EstimatedYearsAdded = estimate.EstimatedYearsAdded,
                        AdjustedLifeExpectancy = estimate.AdjustedLifeExpectancy,
                        EstimatedDeathDate = estimate.EstimatedDeathDate,
                        ConfidenceLevel = estimate.ConfidenceLevel,
                        CalculatedAt = estimate.CalculatedAt,
                        Breakdown = estimate.Breakdown.Select(b => new BreakdownItem
                        {
                            ModelCode = b.ModelCode,
                            ModelName = b.ModelName,
                            YearsAdded = b.YearsAdded,
                            InputValues = b.InputValues,
                            Notes = b.Notes
                        }).ToList(),
                        Recommendations = estimate.Recommendations.Select(r => new RecommendationItem
                        {
                            Area = r.Area,
                            Suggestion = r.Suggestion,
                            PotentialGain = r.PotentialGain
                        }).ToList()
                    }
                }
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to calculate longevity estimate for user {UserId}", userId);
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }
}

public class LongevityResponse
{
    public LongevityData Data { get; set; } = null!;
}

public class LongevityData
{
    public string Type { get; set; } = "longevity";
    public LongevityAttributes Attributes { get; set; } = null!;
}

public class LongevityAttributes
{
    public decimal BaselineLifeExpectancy { get; set; }
    public decimal EstimatedYearsAdded { get; set; }
    public decimal AdjustedLifeExpectancy { get; set; }
    public DateTime? EstimatedDeathDate { get; set; }
    public string ConfidenceLevel { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public List<BreakdownItem> Breakdown { get; set; } = new();
    public List<RecommendationItem> Recommendations { get; set; } = new();
}

public class BreakdownItem
{
    public string ModelCode { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public decimal YearsAdded { get; set; }
    public Dictionary<string, object?> InputValues { get; set; } = new();
    public string? Notes { get; set; }
}

public class RecommendationItem
{
    public string Area { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public decimal PotentialGain { get; set; }
}
