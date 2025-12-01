using LifeOS.Application.Commands.Finances;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Application.Queries.Finances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/investment-contributions")]
[Authorize]
public class InvestmentContributionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvestmentContributionsController> _logger;

    public InvestmentContributionsController(IMediator mediator, ILogger<InvestmentContributionsController> logger)
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
    /// List investment contributions with summary
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(InvestmentContributionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvestmentContributions()
    {
        var result = await _mediator.Send(new GetInvestmentContributionsQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Create investment contribution
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InvestmentContributionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateInvestmentContribution([FromBody] CreateInvestmentContributionRequest request)
    {
        var result = await _mediator.Send(new CreateInvestmentContributionCommand(GetUserId(), request));
        return Created($"/api/investment-contributions/{result.Id}", result);
    }

    /// <summary>
    /// Update investment contribution
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(InvestmentContributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInvestmentContribution(Guid id, [FromBody] UpdateInvestmentContributionRequest request)
    {
        var result = await _mediator.Send(new UpdateInvestmentContributionCommand(GetUserId(), id, request));

        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Investment contribution not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Delete investment contribution
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInvestmentContribution(Guid id)
    {
        var success = await _mediator.Send(new DeleteInvestmentContributionCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Investment contribution not found" } });

        return NoContent();
    }
}
