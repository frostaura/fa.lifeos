using LifeOS.Application.Commands.DataPortability;
using LifeOS.Application.DTOs.DataPortability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace LifeOS.Api.Controllers;

[ApiController]
[Route("api/v1/data")]
[Authorize]
public class DataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DataController> _logger;

    public DataController(IMediator mediator, ILogger<DataController> logger)
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
    /// Export all user data to a single JSON file
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(LifeOSExportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportData()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Invalid user" } });
        }

        _logger.LogInformation("User {UserId} requested data export", userId);

        var result = await _mediator.Send(new ExportDataCommand(userId));

        // Return as file download with proper filename
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss");
        var fileName = $"lifeos-backup-{timestamp}.json";
        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        
        return File(bytes, "application/json", fileName);
    }

    /// <summary>
    /// Import data from a LifeOS export JSON file
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportData([FromBody] ImportRequestDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Invalid user" } });
        }

        if (request.Data == null)
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "Import data is required" } });
        }

        var mode = request.Mode?.ToLowerInvariant();
        if (mode != "replace" && mode != "merge")
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "Mode must be 'replace' or 'merge'" } });
        }

        _logger.LogInformation("User {UserId} requested data import in {Mode} mode (DryRun: {DryRun})", 
            userId, mode, request.DryRun);

        try
        {
            var result = await _mediator.Send(new ImportDataCommand(
                userId, 
                request.Data, 
                mode, 
                request.DryRun));

            return Ok(new { data = result });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("schema"))
        {
            return UnprocessableEntity(new { 
                error = new { 
                    code = "SCHEMA_INCOMPATIBLE", 
                    message = ex.Message 
                } 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed for user {UserId}", userId);
            return UnprocessableEntity(new { 
                error = new { 
                    code = "IMPORT_FAILED", 
                    message = "Import failed: " + ex.Message 
                } 
            });
        }
    }

    /// <summary>
    /// Import data from multipart form upload
    /// </summary>
    [HttpPost("import/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportDataUpload(
        IFormFile file, 
        [FromForm] string mode = "replace",
        [FromForm] bool dryRun = false)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Invalid user" } });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "File is required" } });
        }

        var normalizedMode = mode?.ToLowerInvariant();
        if (normalizedMode != "replace" && normalizedMode != "merge")
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "Mode must be 'replace' or 'merge'" } });
        }

        _logger.LogInformation("User {UserId} uploading data file {FileName} ({FileSize} bytes) in {Mode} mode (DryRun: {DryRun})", 
            userId, file.FileName, file.Length, normalizedMode, dryRun);

        try
        {
            using var stream = file.OpenReadStream();
            var exportData = await JsonSerializer.DeserializeAsync<LifeOSExportDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (exportData == null)
            {
                return BadRequest(new { error = new { code = "BAD_REQUEST", message = "Invalid JSON file format" } });
            }

            var result = await _mediator.Send(new ImportDataCommand(
                userId, 
                exportData, 
                normalizedMode, 
                dryRun));

            return Ok(new { data = result });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { 
                error = new { 
                    code = "INVALID_JSON", 
                    message = "Failed to parse JSON file: " + ex.Message 
                } 
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("schema"))
        {
            return UnprocessableEntity(new { 
                error = new { 
                    code = "SCHEMA_INCOMPATIBLE", 
                    message = ex.Message 
                } 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import upload failed for user {UserId}", userId);
            return UnprocessableEntity(new { 
                error = new { 
                    code = "IMPORT_FAILED", 
                    message = "Import failed: " + ex.Message 
                } 
            });
        }
    }
}
