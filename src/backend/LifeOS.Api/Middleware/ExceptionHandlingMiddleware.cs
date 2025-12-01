using System.Net;
using System.Text.Json;

namespace LifeOS.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        _logger.LogError(exception, "Unhandled exception occurred. RequestId: {RequestId}", requestId);

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, errorCode, message) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Authentication required"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", "Resource not found"),
            InvalidOperationException => (HttpStatusCode.Conflict, "CONFLICT", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred")
        };

        response.StatusCode = (int)statusCode;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = errorCode,
                Message = _env.IsDevelopment() ? exception.Message : message,
                TraceId = requestId,
                Details = _env.IsDevelopment() ? GetExceptionDetails(exception) : null
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(json);
    }

    private static List<ErrorFieldDetail>? GetExceptionDetails(Exception ex)
    {
        return new List<ErrorFieldDetail>
        {
            new() { Field = "exception", Code = ex.GetType().Name, Message = ex.Message },
            new() { Field = "stackTrace", Code = "DEBUG", Message = ex.StackTrace ?? "" }
        };
    }
}

public class ErrorResponse
{
    public ErrorDetail Error { get; set; } = new();
}

public class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public List<ErrorFieldDetail>? Details { get; set; }
}

public class ErrorFieldDetail
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
