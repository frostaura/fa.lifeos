using LifeOS.Application.Commands.Simulations;
using LifeOS.Application.DTOs.Simulations;
using LifeOS.Application.Queries.Simulations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/simulations")]
[Authorize]
public class SimulationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(IMediator mediator, ILogger<SimulationsController> logger)
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

    #region Scenarios

    /// <summary>
    /// List all simulation scenarios
    /// </summary>
    [HttpGet("scenarios")]
    [ProducesResponseType(typeof(ScenarioListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenarios()
    {
        var result = await _mediator.Send(new GetScenariosQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Get a single simulation scenario
    /// </summary>
    [HttpGet("scenarios/{id:guid}")]
    [ProducesResponseType(typeof(ScenarioDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScenarioById(Guid id)
    {
        var result = await _mediator.Send(new GetScenarioByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Scenario not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create a new simulation scenario
    /// </summary>
    [HttpPost("scenarios")]
    [ProducesResponseType(typeof(ScenarioDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateScenario([FromBody] CreateScenarioRequest request)
    {
        var result = await _mediator.Send(new CreateScenarioCommand(
            GetUserId(),
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.EndCondition,
            request.BaseAssumptions,
            request.IsBaseline));

        return CreatedAtAction(nameof(GetScenarioById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update a simulation scenario
    /// </summary>
    [HttpPatch("scenarios/{id:guid}")]
    [ProducesResponseType(typeof(ScenarioDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScenario(Guid id, [FromBody] UpdateScenarioRequest request)
    {
        var success = await _mediator.Send(new UpdateScenarioCommand(
            GetUserId(),
            id,
            request.Name,
            request.Description,
            request.EndDate,
            request.EndCondition,
            request.BaseAssumptions,
            request.IsBaseline));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Scenario not found" } });

        var result = await _mediator.Send(new GetScenarioByIdQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Delete a simulation scenario
    /// </summary>
    [HttpDelete("scenarios/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScenario(Guid id)
    {
        var success = await _mediator.Send(new DeleteScenarioCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Scenario not found" } });

        return NoContent();
    }

    /// <summary>
    /// Run a simulation and generate projections
    /// </summary>
    [HttpPost("scenarios/{id:guid}/run")]
    [ProducesResponseType(typeof(RunSimulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunSimulation(Guid id, [FromBody] RunSimulationRequest? request)
    {
        try
        {
            var result = await _mediator.Send(new RunSimulationCommand(
                GetUserId(),
                id,
                request?.RecalculateFromStart ?? true));

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get projections for a simulation scenario
    /// </summary>
    [HttpGet("scenarios/{id:guid}/projections")]
    [ProducesResponseType(typeof(ProjectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjections(
        Guid id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string granularity = "monthly",
        [FromQuery] Guid? accountId = null)
    {
        try
        {
            var result = await _mediator.Send(new GetProjectionsQuery(
                GetUserId(),
                id,
                from,
                to,
                granularity,
                accountId));

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// List simulation events
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(EventListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] Guid? scenarioId)
    {
        var result = await _mediator.Send(new GetEventsQuery(GetUserId(), scenarioId));
        return Ok(result);
    }

    /// <summary>
    /// Get a single simulation event
    /// </summary>
    [HttpGet("events/{id:guid}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Event not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create a simulation event
    /// </summary>
    [HttpPost("events")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            var result = await _mediator.Send(new CreateEventCommand(
                GetUserId(),
                request.ScenarioId,
                request.Name,
                request.Description,
                request.TriggerType,
                request.TriggerDate,
                request.TriggerAge,
                request.TriggerCondition,
                request.EventType,
                request.Currency,
                request.AmountType,
                request.AmountValue,
                request.AmountFormula,
                request.AffectedAccountId,
                request.AppliesOnce,
                request.RecurrenceFrequency,
                request.RecurrenceEndDate,
                request.SortOrder));

            return CreatedAtAction(nameof(GetEventById), new { id = result.Data.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Update a simulation event
    /// </summary>
    [HttpPatch("events/{id:guid}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        var success = await _mediator.Send(new UpdateEventCommand(
            GetUserId(),
            id,
            request.Name,
            request.Description,
            request.TriggerType,
            request.TriggerDate,
            request.TriggerAge,
            request.TriggerCondition,
            request.EventType,
            request.Currency,
            request.AmountType,
            request.AmountValue,
            request.AmountFormula,
            request.AffectedAccountId,
            request.AppliesOnce,
            request.RecurrenceFrequency,
            request.RecurrenceEndDate,
            request.SortOrder,
            request.IsActive));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Event not found" } });

        var result = await _mediator.Send(new GetEventByIdQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Delete a simulation event
    /// </summary>
    [HttpDelete("events/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var success = await _mediator.Send(new DeleteEventCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Event not found" } });

        return NoContent();
    }

    #endregion
}
