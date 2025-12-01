using LifeOS.Application.Commands.Tasks;
using LifeOS.Application.DTOs.Tasks;
using LifeOS.Application.Queries.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TasksController> _logger;

    public TasksController(IMediator mediator, ILogger<TasksController> logger)
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
    /// List tasks with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string? taskType,
        [FromQuery] Guid? dimensionId,
        [FromQuery] Guid? milestoneId,
        [FromQuery] bool? isCompleted,
        [FromQuery] bool? isActive,
        [FromQuery] DateOnly? scheduledFrom,
        [FromQuery] DateOnly? scheduledTo,
        [FromQuery] string? tags,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var tagArray = string.IsNullOrEmpty(tags) ? null : tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var result = await _mediator.Send(new GetTasksQuery(
            GetUserId(),
            taskType,
            dimensionId,
            milestoneId,
            isCompleted,
            isActive,
            scheduledFrom,
            scheduledTo,
            tagArray,
            page,
            Math.Min(perPage, 100)));

        return Ok(result);
    }

    /// <summary>
    /// Get task with streak info
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(Guid id)
    {
        var result = await _mediator.Send(new GetTaskByIdQuery(GetUserId(), id));
        
        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Task not found" } });

        return Ok(result);
    }

    /// <summary>
    /// Create task/habit
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var result = await _mediator.Send(new CreateTaskCommand(
            GetUserId(),
            request.Title,
            request.Description,
            request.TaskType,
            request.Frequency,
            request.DimensionId,
            request.MilestoneId,
            request.LinkedMetricCode,
            request.ScheduledDate,
            request.ScheduledTime,
            request.StartDate,
            request.EndDate,
            request.Tags));

        return CreatedAtAction(nameof(GetTaskById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Update task
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var success = await _mediator.Send(new UpdateTaskCommand(
            GetUserId(),
            id,
            request.Title,
            request.Description,
            request.Frequency,
            request.ScheduledDate,
            request.ScheduledTime,
            request.EndDate,
            request.IsActive,
            request.Tags));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Task not found" } });

        var result = await _mediator.Send(new GetTaskByIdQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Delete task
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var success = await _mediator.Send(new DeleteTaskCommand(GetUserId(), id));

        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Task not found" } });

        return NoContent();
    }

    /// <summary>
    /// Mark task as complete, update streak
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteTask(Guid id, [FromBody] CompleteTaskRequest? request)
    {
        var result = await _mediator.Send(new CompleteTaskCommand(
            GetUserId(),
            id,
            request?.CompletedAt,
            request?.MetricValue));

        if (result == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Task not found" } });

        return Ok(result);
    }
}
