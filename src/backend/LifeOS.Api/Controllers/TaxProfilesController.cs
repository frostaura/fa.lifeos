using LifeOS.Application.Commands.Finances;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Application.Queries.Finances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/tax-profiles")]
[Authorize]
public class TaxProfilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TaxProfilesController> _logger;

    public TaxProfilesController(IMediator mediator, ILogger<TaxProfilesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// List tax profiles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TaxProfileListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxProfiles()
    {
        var result = await _mediator.Send(new GetTaxProfilesQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Create tax profile
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaxProfileDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTaxProfile([FromBody] CreateTaxProfileRequest request)
    {
        var result = await _mediator.Send(new CreateTaxProfileCommand(
            GetUserId(),
            request.Name,
            request.TaxYear,
            request.CountryCode,
            request.Brackets,
            request.UifRate,
            request.UifCap,
            request.VatRate,
            request.IsVatRegistered,
            request.TaxRebates));

        return Created($"/api/tax-profiles/{result.Data.Id}", result);
    }

    /// <summary>
    /// Update tax profile
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(TaxProfileListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTaxProfile(Guid id, [FromBody] UpdateTaxProfileRequest request)
    {
        var success = await _mediator.Send(new UpdateTaxProfileCommand(
            GetUserId(),
            id,
            request.Name,
            request.Brackets,
            request.UifRate,
            request.UifCap,
            request.VatRate,
            request.IsVatRegistered,
            request.TaxRebates,
            request.IsActive));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Tax profile not found" } });

        var result = await _mediator.Send(new GetTaxProfilesQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Delete tax profile
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTaxProfile(Guid id)
    {
        var success = await _mediator.Send(new DeleteTaxProfileCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Tax profile not found" } });

        return NoContent();
    }
}
