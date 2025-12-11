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

    #region v1.1 Scenario Comparison

    /// <summary>
    /// v1.1: Compare multiple scenarios against baseline
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompareScenarios([FromBody] ScenarioComparisonRequest request)
    {
        var userId = GetUserId();

        // Get baseline scenario
        var baselineResult = await _mediator.Send(new GetScenarioByIdQuery(userId, request.BaselineScenarioId));
        if (baselineResult == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Baseline scenario not found" } });
        }

        // Get baseline projections
        var horizonYears = request.HorizonYears > 0 ? request.HorizonYears : 10;
        var toDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(horizonYears));
        var baselineProjections = await _mediator.Send(new GetProjectionsQuery(
            userId, request.BaselineScenarioId, null, toDate, "monthly", null));
        var baselineEndNetWorth = baselineProjections?.Data?.Summary?.EndNetWorth ?? 0;

        var comparisons = new List<object>();

        foreach (var compareId in request.CompareScenarioIds ?? new List<Guid>())
        {
            var compareResult = await _mediator.Send(new GetScenarioByIdQuery(userId, compareId));
            if (compareResult != null)
            {
                // Get projections for comparison
                var projections = await _mediator.Send(new GetProjectionsQuery(
                    userId, compareId, null, toDate, "monthly", null));
                var endNetWorth = projections?.Data?.Summary?.EndNetWorth ?? 0;

                comparisons.Add(new
                {
                    scenarioId = compareId,
                    scenarioName = compareResult.Data?.Attributes?.Name ?? "Unknown",
                    endNetWorth,
                    netWorthDelta = endNetWorth - baselineEndNetWorth,
                    milestoneYears = CalculateMilestoneYears(projections, request.MilestoneTargets ?? new List<decimal>())
                });
            }
        }

        return Ok(new
        {
            data = new
            {
                baseline = new
                {
                    scenarioId = request.BaselineScenarioId,
                    scenarioName = baselineResult.Data?.Attributes?.Name ?? "Baseline",
                    endNetWorth = baselineEndNetWorth,
                    milestoneYears = CalculateMilestoneYears(baselineProjections, request.MilestoneTargets ?? new List<decimal>())
                },
                comparisons
            }
        });
    }

    /// <summary>
    /// v1.1: Quick what-if calculation for a one-off purchase
    /// </summary>
    [HttpGet("what-if")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> WhatIfAnalysis(
        [FromQuery] decimal purchaseAmount,
        [FromQuery] DateTime? purchaseDate,
        [FromQuery] Guid? scenarioId)
    {
        var userId = GetUserId();

        // Get baseline or specified scenario
        Guid effectiveScenarioId;
        if (scenarioId.HasValue)
        {
            effectiveScenarioId = scenarioId.Value;
        }
        else
        {
            // Find baseline scenario
            var scenarios = await _mediator.Send(new GetScenariosQuery(userId));
            var baseline = scenarios?.Data?.FirstOrDefault(s => s.Attributes?.IsBaseline == true);
            if (baseline == null)
            {
                return NotFound(new { error = new { code = "NOT_FOUND", message = "No baseline scenario found" } });
            }
            effectiveScenarioId = baseline.Id;
        }

        // Get projections for the scenario (10 years out)
        var toDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10));
        var projections = await _mediator.Send(new GetProjectionsQuery(
            userId, effectiveScenarioId, null, toDate, "monthly", null));

        var monthlyData = projections?.Data?.MonthlyProjections ?? new List<MonthlyProjection>();
        
        // Get 10-year net worth
        var netWorthWithoutPurchase = projections?.Data?.Summary?.EndNetWorth ?? 0;
        var startNetWorth = projections?.Data?.Summary?.StartNetWorth ?? 0;

        // Find first million date
        DateOnly? firstMillionWithout = null;
        foreach (var month in monthlyData)
        {
            if (month.NetWorth >= 1_000_000 && DateOnly.TryParse(month.Period + "-01", out var date))
            {
                firstMillionWithout = date;
                break;
            }
        }

        // Estimate with purchase (simple compound reduction)
        var annualGrowthRate = 0.07m;
        var yearsToCompound = 10m;
        var purchaseImpact = purchaseAmount * (decimal)Math.Pow((double)(1 + annualGrowthRate), (double)yearsToCompound);
        var netWorthWithPurchase = netWorthWithoutPurchase - purchaseImpact;

        // Estimate delay in reaching first million
        DateOnly? firstMillionWith = null;
        string milestoneDelay = "0 months";
        if (firstMillionWithout.HasValue && monthlyData.Count > 1)
        {
            // Estimate monthly savings from projection data
            var totalMonths = monthlyData.Count;
            var netGrowth = netWorthWithoutPurchase - startNetWorth;
            var monthlySavings = totalMonths > 0 ? netGrowth / totalMonths : 0;
            
            if (monthlySavings > 0)
            {
                var delayMonths = (int)(purchaseAmount / monthlySavings);
                firstMillionWith = firstMillionWithout.Value.AddMonths(delayMonths);
                milestoneDelay = $"{delayMonths} months";
            }
        }

        return Ok(new
        {
            data = new
            {
                withoutPurchase = new
                {
                    netWorthAt10Years = netWorthWithoutPurchase,
                    firstMillionDate = firstMillionWithout?.ToString("yyyy-MM-dd")
                },
                withPurchase = new
                {
                    netWorthAt10Years = netWorthWithPurchase,
                    firstMillionDate = firstMillionWith?.ToString("yyyy-MM-dd"),
                    impact = new
                    {
                        netWorthReduction = purchaseImpact,
                        milestoneDelay
                    }
                }
            }
        });
    }

    private static Dictionary<string, decimal?> CalculateMilestoneYears(
        ProjectionResponse? projections,
        List<decimal> milestoneTargets)
    {
        var result = new Dictionary<string, decimal?>();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthlyData = projections?.Data?.MonthlyProjections ?? new List<MonthlyProjection>();

        foreach (var target in milestoneTargets)
        {
            DateOnly? milestoneDate = null;
            foreach (var month in monthlyData)
            {
                if (month.NetWorth >= target && DateOnly.TryParse(month.Period + "-01", out var date))
                {
                    milestoneDate = date;
                    break;
                }
            }
            
            if (milestoneDate.HasValue)
            {
                var years = (milestoneDate.Value.DayNumber - startDate.DayNumber) / 365.25m;
                result[target.ToString()] = Math.Round(years, 1);
            }
            else
            {
                result[target.ToString()] = null;
            }
        }

        return result;
    }

    #endregion
}

/// <summary>
/// v1.1: Scenario comparison request
/// </summary>
public record ScenarioComparisonRequest
{
    public Guid BaselineScenarioId { get; init; }
    public List<Guid>? CompareScenarioIds { get; init; }
    public int HorizonYears { get; init; } = 10;
    public List<decimal>? MilestoneTargets { get; init; }
}
