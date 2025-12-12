using LifeOS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/longevity-models")]
[Authorize]
public class LongevityModelsController : ControllerBase
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<LongevityModelsController> _logger;

    public LongevityModelsController(
        ILifeOSDbContext context,
        ILogger<LongevityModelsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all longevity models
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LongevityModelsListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModels(CancellationToken cancellationToken)
    {
        var models = await _context.LongevityModels
            .OrderBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var response = new LongevityModelsListResponse
        {
            Data = models.Select(m => new LongevityModelItem
            {
                Id = m.Id.ToString(),
                Type = "longevity-model",
                Attributes = new LongevityModelAttributes
                {
                    UserId = m.UserId,
                    Code = m.Code,
                    Name = m.Name,
                    Description = m.Description,
                    InputMetrics = System.Text.Json.JsonSerializer.Deserialize<string[]>(m.InputMetrics) ?? Array.Empty<string>(),
                    ModelType = m.ModelType.ToString(),
                    Parameters = m.Parameters,
                    MaxRiskReduction = m.MaxRiskReduction,
                    SourceCitation = m.SourceCitation,
                    SourceUrl = m.SourceUrl,
                    IsActive = m.IsActive
                }
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Update a longevity model
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateModel(
        Guid id, 
        [FromBody] UpdateLongevityModelRequest request,
        CancellationToken cancellationToken)
    {
        var model = await _context.LongevityModels
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (model == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Longevity model not found" } });
        }

        if (request.Name != null)
            model.Name = request.Name;
        if (request.Description != null)
            model.Description = request.Description;
        if (request.Parameters != null)
            model.Parameters = request.Parameters;
        if (request.IsActive.HasValue)
            model.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public class LongevityModelsListResponse
{
    public List<LongevityModelItem> Data { get; set; } = new();
}

public class LongevityModelItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "longevity-model";
    public LongevityModelAttributes Attributes { get; set; } = null!;
}

public class LongevityModelAttributes
{
    public Guid? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[] InputMetrics { get; set; } = Array.Empty<string>();
    public string ModelType { get; set; } = string.Empty;
    public string Parameters { get; set; } = "{}";
    public decimal MaxRiskReduction { get; set; }
    public string? SourceCitation { get; set; }
    public string? SourceUrl { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateLongevityModelRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Parameters { get; set; }
    public bool? IsActive { get; set; }
}
