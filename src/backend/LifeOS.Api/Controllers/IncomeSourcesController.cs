using LifeOS.Application.Commands.Finances;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Application.Queries.Finances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/income-sources")]
[Authorize]
public class IncomeSourcesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IncomeSourcesController> _logger;

    public IncomeSourcesController(IMediator mediator, ILogger<IncomeSourcesController> logger)
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
    /// List income sources
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IncomeSourceListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomeSources()
    {
        var result = await _mediator.Send(new GetIncomeSourcesQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Create income source
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IncomeSourceDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateIncomeSource([FromBody] CreateIncomeSourceRequest request)
    {
        var result = await _mediator.Send(new CreateIncomeSourceCommand(
            GetUserId(),
            request.Name,
            request.Currency,
            request.BaseAmount,
            request.IsPreTax,
            request.TaxProfileId,
            request.PaymentFrequency,
            request.NextPaymentDate,
            request.AnnualIncreaseRate,
            request.EmployerName,
            request.Notes,
            request.TargetAccountId,
            request.EndConditionType,
            request.EndConditionAccountId,
            request.EndDate,
            request.EndAmountThreshold));

        return Created($"/api/income-sources/{result.Data.Id}", result);
    }

    /// <summary>
    /// Update income source
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(IncomeSourceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIncomeSource(Guid id, [FromBody] UpdateIncomeSourceRequest request)
    {
        var success = await _mediator.Send(new UpdateIncomeSourceCommand(
            GetUserId(),
            id,
            request.Name,
            request.BaseAmount,
            request.TaxProfileId,
            request.ClearTaxProfile,
            request.PaymentFrequency,
            request.NextPaymentDate,
            request.AnnualIncreaseRate,
            request.EmployerName,
            request.Notes,
            request.IsActive,
            request.TargetAccountId,
            request.EndConditionType,
            request.EndConditionAccountId,
            request.EndDate,
            request.EndAmountThreshold));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Income source not found" } });

        var result = await _mediator.Send(new GetIncomeSourcesQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Delete income source
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIncomeSource(Guid id)
    {
        var success = await _mediator.Send(new DeleteIncomeSourceCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Income source not found" } });

        return NoContent();
    }
}
