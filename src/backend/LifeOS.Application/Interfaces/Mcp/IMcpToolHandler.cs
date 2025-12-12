using LifeOS.Application.DTOs.Mcp;

namespace LifeOS.Application.Interfaces.Mcp;

/// <summary>
/// Interface for MCP tool handlers. Each tool implements this interface to provide
/// a standardized JSON-in/JSON-out operation following Model Context Protocol conventions.
/// </summary>
public interface IMcpToolHandler
{
    /// <summary>
    /// Unique name of the tool (e.g., "lifeos.getDashboardSnapshot")
    /// </summary>
    string ToolName { get; }
    
    /// <summary>
    /// Optional description of what the tool does
    /// </summary>
    string? Description => null;
    
    /// <summary>
    /// Handles the tool execution with JSON input and returns structured output
    /// </summary>
    /// <param name="jsonInput">Optional JSON input string</param>
    /// <param name="userId">User ID from JWT claims</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Structured response with success/data/error</returns>
    Task<McpToolResponse<object>> HandleAsync(string? jsonInput, Guid userId, CancellationToken cancellationToken);
}
