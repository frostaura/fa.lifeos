namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// Standardized response wrapper for all MCP tools following Model Context Protocol conventions.
/// Provides consistent JSON-in/JSON-out interface for AI systems.
/// </summary>
/// <typeparam name="T">The type of data returned by the tool</typeparam>
public class McpToolResponse<T>
{
    /// <summary>
    /// Indicates whether the tool execution succeeded
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The data returned by the tool if successful, null otherwise
    /// </summary>
    public T? Data { get; set; }
    
    /// <summary>
    /// Error message if the tool execution failed, null otherwise
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static McpToolResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
        Error = null
    };
    
    /// <summary>
    /// Creates a failed response with error message
    /// </summary>
    public static McpToolResponse<T> Fail(string error) => new()
    {
        Success = false,
        Data = default,
        Error = error
    };
}
