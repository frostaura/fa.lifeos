using LifeOS.Application.Commands.Accounts;
using LifeOS.Application.DTOs.Accounts;
using LifeOS.Application.Queries.Accounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IMediator mediator, ILogger<AccountsController> logger)
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
    /// List accounts with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AccountListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] string? accountType = null,
        [FromQuery] string? currency = null,
        [FromQuery] bool? isActive = true,
        [FromQuery] bool? isLiability = null,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var result = await _mediator.Send(new GetAccountsQuery(
            GetUserId(),
            accountType,
            currency,
            isActive,
            isLiability,
            page,
            Math.Min(perPage, 100)));

        return Ok(result);
    }

    /// <summary>
    /// Get single account
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var result = await _mediator.Send(new GetAccountByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Account not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Get account balance with FX conversion
    /// </summary>
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(AccountBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountBalance(Guid id, [FromQuery] string? currency)
    {
        var result = await _mediator.Send(new GetAccountBalanceQuery(GetUserId(), id, currency));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Account not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create account
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var result = await _mediator.Send(new CreateAccountCommand(
            GetUserId(),
            request.Name,
            request.AccountType,
            request.Currency,
            request.InitialBalance,
            request.Institution,
            request.IsLiability,
            request.InterestRateAnnual,
            request.InterestCompounding,
            request.MonthlyFee,
            request.Metadata));

        return CreatedAtAction(nameof(GetAccountById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update account
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request)
    {
        var success = await _mediator.Send(new UpdateAccountCommand(
            GetUserId(),
            id,
            request.Name,
            request.AccountType,
            request.Currency,
            request.CurrentBalance,
            request.Institution,
            request.IsLiability,
            request.InterestRateAnnual,
            request.InterestCompounding,
            request.MonthlyFee,
            request.Metadata,
            request.IsActive));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Account not found" } });

        var result = await _mediator.Send(new GetAccountByIdQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Delete account (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var success = await _mediator.Send(new DeleteAccountCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Account not found" } });

        return NoContent();
    }
}
