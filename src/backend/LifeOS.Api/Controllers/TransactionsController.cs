using LifeOS.Application.Commands.Transactions;
using LifeOS.Application.DTOs.Transactions;
using LifeOS.Application.Queries.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IMediator mediator, ILogger<TransactionsController> logger)
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
    /// List transactions with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TransactionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? accountId,
        [FromQuery] string? category,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? tags,
        [FromQuery] bool? isReconciled,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var tagArray = string.IsNullOrEmpty(tags) ? null : tags.Split(',');
        
        var result = await _mediator.Send(new GetTransactionsQuery(
            GetUserId(),
            accountId,
            category,
            from.HasValue ? DateOnly.FromDateTime(from.Value) : null,
            to.HasValue ? DateOnly.FromDateTime(to.Value) : null,
            minAmount,
            maxAmount,
            tagArray,
            isReconciled,
            sort,
            page,
            Math.Min(perPage, 100)));

        return Ok(result);
    }

    /// <summary>
    /// Get single transaction
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var result = await _mediator.Send(new GetTransactionByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Transaction not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create transaction
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        if (request.SourceAccountId == null && request.TargetAccountId == null)
        {
            return UnprocessableEntity(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "At least one account (source or target) must be specified"
                }
            });
        }

        var result = await _mediator.Send(new CreateTransactionCommand(
            GetUserId(),
            request.SourceAccountId,
            request.TargetAccountId,
            request.Currency,
            request.Amount,
            request.Category,
            request.Subcategory,
            request.Tags,
            request.Description,
            request.Notes,
            request.TransactionDate));

        return CreatedAtAction(nameof(GetTransactionById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update transaction
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionRequest request)
    {
        var success = await _mediator.Send(new UpdateTransactionCommand(
            GetUserId(),
            id,
            request.Subcategory,
            request.Tags,
            request.Description,
            request.Notes,
            request.IsReconciled));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Transaction not found" } });

        var result = await _mediator.Send(new GetTransactionByIdQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Delete transaction
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var success = await _mediator.Send(new DeleteTransactionCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Transaction not found" } });

        return NoContent();
    }
}
